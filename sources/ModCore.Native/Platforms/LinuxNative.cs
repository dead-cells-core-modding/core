extern alias iced;
using Hashlink;
using iced::Iced.Intel;
using ModCore.Storage;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static iced::Iced.Intel.AssemblerRegisters;

namespace ModCore.Native
{
    [SupportedOSPlatform("linux")]
    internal unsafe partial class LinuxNative : Native
    {
        // ── mprotect constants ──────────────────────────────────────────
        private const int PROT_READ  = 0x1;
        private const int PROT_WRITE = 0x2;
        private const int PROT_EXEC  = 0x4;
        private const int PROT_NONE  = 0x0;

        // ── syscall numbers (x86_64) ────────────────────────────────────
        private const long SYS_gettid = 186;

        // ── Statically-resolved native symbols ──────────────────────────
        // Stored via NativeMemory.Alloc so generated asm can reference them
        // by absolute address without fighting .NET static-field indirection.
        private static readonly nint* _pthreadGetspecificPtr;
        private static readonly int*  _pthreadKeyPtr;
        private static readonly nint* _pthreadSelfPtr;
        private static readonly nint* _mprotectPtr;
        private static readonly nint* _syscallPtr;

        // Dedicated lock objects (pointers aren't reference types in C#).
        private static readonly Lock _keyLock  = new();
        private static readonly Lock _mapsLock = new();

        static LinuxNative()
        {
            if (!NativeLibrary.TryLoad("libc.so.6", out nint libc))
                libc = NativeLibrary.Load("libc.so.6");

            _pthreadGetspecificPtr = (nint*)NativeMemory.Alloc((nuint)sizeof(nint));
            *_pthreadGetspecificPtr = NativeLibrary.GetExport(libc, "pthread_getspecific");

            _pthreadSelfPtr = (nint*)NativeMemory.Alloc((nuint)sizeof(nint));
            *_pthreadSelfPtr = NativeLibrary.GetExport(libc, "pthread_self");

            _mprotectPtr = (nint*)NativeMemory.Alloc((nuint)sizeof(nint));
            *_mprotectPtr = NativeLibrary.GetExport(libc, "mprotect");

            _syscallPtr = (nint*)NativeMemory.Alloc((nuint)sizeof(nint));
            *_syscallPtr = NativeLibrary.GetExport(libc, "syscall");

            _pthreadKeyPtr = (int*)NativeMemory.Alloc((nuint)sizeof(int));
            *_pthreadKeyPtr = -1;
        }

        // ── Library loading ─────────────────────────────────────────────

        public override bool TryLoadLibrary(string path, out nint handle)
        {
            if (NativeLibrary.TryLoad(path, out handle))
                return true;
            if (NativeLibrary.TryLoad(path + ".so", out handle))
                return true;
            if (NativeLibrary.TryLoad(FolderInfo.CurrentNativeRoot.GetFilePath(path), out handle))
                return true;
            if (NativeLibrary.TryLoad(FolderInfo.CurrentNativeRoot.GetFilePath(path + ".so"), out handle))
                return true;
            return false;
        }

        // ── Native hooks ─────────────────────────────────────────────────

        /// <summary>Suppress default Hashlink throw handling — we manage it ourselves.</summary>
        [UnmanagedCallersOnly]
        private static int Hook_throw_handler(int code) => 0;

        protected override void InitializeNativeHooks()
        {
            base.InitializeNativeHooks();
        }

        // ── Assembly entry point (called before InitializeAsm) ──────────

        protected override void InitializeAsm()
        {
            // Ensure TLS key is allocated before base generates asm.
            if (*_pthreadKeyPtr < 0)
                AllocPthreadKey();

            base.InitializeAsm();
        }

        // ── TLS (pthread_key) ───────────────────────────────────────────

        private static void AllocPthreadKey()
        {
            lock (_keyLock)
            {
                if (*_pthreadKeyPtr >= 0) return;

                int key;
                // pthread_key_create — first arg in rdi (pointer to key)
                var createKey = (delegate* unmanaged<int*, nint, int>)
                    NativeLibrary.GetExport(
                        NativeLibrary.Load("libc.so.6"),
                        "pthread_key_create");
                int rc = createKey(&key, 0);
                if (rc != 0)
                    throw new InvalidOperationException($"pthread_key_create failed: {rc}");
                *_pthreadKeyPtr = key;
            }
        }

        public override int AllocTls()
        {
            if (*_pthreadKeyPtr < 0)
                AllocPthreadKey();
            return *_pthreadKeyPtr;
        }

        public override nint GetTlsValue(int index)
        {
            var fn = (delegate* unmanaged<int, nint>)*_pthreadGetspecificPtr;
            return fn(index);
        }

        public override void SetTlsValue(int index, nint val)
        {
            var fn = (delegate* unmanaged<int, nint, int>)
                NativeLibrary.GetExport(
                    NativeLibrary.Load("libc.so.6"),
                    "pthread_setspecific");
            fn(index, val);
        }

        // ── Memory page protection ──────────────────────────────────────
        //  Cache the original protection per page so RestorePageProtect
        //  can put it back.  mprotect() itself does not return the old
        //  value, so we derive a reasonable default when the cache misses.

        private static readonly Dictionary<nint, int> _pageProtCache = new();

        public override void MakePageWritable(nint ptr, out int old)
        {
            nint pageStart = ptr & ~(Environment.SystemPageSize - 1);
            lock (_pageProtCache)
            {
                if (!_pageProtCache.TryGetValue(pageStart, out old))
                {
                    // Default assumption for JIT code pages: R + X
                    old = PROT_READ | PROT_EXEC;
                }
            }

            var mprotect = (delegate* unmanaged<nint, nuint, int, int>)*_mprotectPtr;
            mprotect(pageStart, (nuint)Environment.SystemPageSize,
                     PROT_READ | PROT_WRITE | PROT_EXEC);
        }

        public override void RestorePageProtect(nint ptr, int val)
        {
            nint pageStart = ptr & ~(Environment.SystemPageSize - 1);
            var mprotect = (delegate* unmanaged<nint, nuint, int, int>)*_mprotectPtr;
            mprotect(pageStart, (nuint)Environment.SystemPageSize, val);

            lock (_pageProtCache)
            {
                _pageProtCache[pageStart] = val;
            }
        }

        // ── Stack helpers ───────────────────────────────────────────────

        public override nint GetCurrentThreadStackBase()
        {
            // pthread_getattr_np + pthread_attr_getstack
            var self = (delegate* unmanaged<nint>)*_pthreadSelfPtr;

            nint attrStorage = 0;
            var getattr = (delegate* unmanaged<nint, nint*, int>)
                NativeLibrary.GetExport(
                    NativeLibrary.Load("libc.so.6"),
                    "pthread_getattr_np");
            if (getattr(self(), &attrStorage) != 0)
                return 0;

            nint stackAddr = 0;
            nuint stackSize = 0;
            var getstack = (delegate* unmanaged<nint, nint*, nuint*, int>)
                NativeLibrary.GetExport(
                    NativeLibrary.Load("libc.so.6"),
                    "pthread_attr_getstack");
            getstack(attrStorage, &stackAddr, &stackSize);

            var destroy = (delegate* unmanaged<nint, int>)
                NativeLibrary.GetExport(
                    NativeLibrary.Load("libc.so.6"),
                    "pthread_attr_destroy");
            destroy(attrStorage);

            // Stack grows downward on x86_64 — top (base) is the high address.
            return stackAddr + (nint)stackSize;
        }

        public override void FixThreadCurrentStackFrame(HL_thread_info* t)
        {
            if (!Environment.Is64BitProcess)
                throw new PlatformNotSupportedException();

            // Current thread: simple self-reference.
            int myTid = (int)GetCurrentThreadId();
            if (t->thread_id == myTid)
            {
                t->stack_cur = &t;
                return;
            }

            // Remote thread: use pthread_getattr_np to get its stack range,
            // then set stack_cur to a conservative (high) address within it.
            //
            // On Linux t->thread_id is typically the kernel TID (gettid()),
            // NOT a pthread_t, so we cannot directly call pthread_getattr_np.
            // As a fallback we approximate via /proc/self/task/<tid>/stat
            // to read the kernel-reported stack pointer (field 28: kstkesp).
            nint approxStack = ReadThreadKstkEsp(t->thread_id);
            if (approxStack != 0)
            {
                t->stack_cur = (void*)approxStack;
            }
            else
            {
                // Last resort: mark the whole stack range valid.
                t->stack_cur = (void*)GetCurrentThreadStackBase();
            }
        }

        /// <summary>
        /// Read the 28th field (kstkesp — kernel-reported userland stack
        /// pointer) from /proc/self/task/<tid>/stat.  Returns 0 on failure.
        /// </summary>
        private static nint ReadThreadKstkEsp(int tid)
        {
            try
            {
                string stat = File.ReadAllText($"/proc/self/task/{tid}/stat");
                // Fields are space-separated.  Field 2 (comm) may contain
                // spaces inside parentheses, so parse from the right.
                int closeParen = stat.LastIndexOf(')');
                if (closeParen < 0) return 0;
                // After ") " there are 25 more fields; we want the 28th
                // overall = (28 - 2 - 1) = 25th after the closing paren.
                var tail = stat.AsSpan(closeParen + 2); // skip ") "
                int field = 0;
                int start = 0;
                for (int i = 0; i < tail.Length; i++)
                {
                    if (tail[i] == ' ')
                    {
                        if (field == 25) // kstkesp is field 28 (0-indexed: 27)
                        {
                            return nint.Parse(tail[start..i]);
                        }
                        field++;
                        start = i + 1;
                    }
                }
            }
            catch { }
            return 0;
        }

        /// <summary>
        /// Get the kernel TID for the calling thread.
        /// </summary>
        private static int GetCurrentThreadId()
        {
            var syscall = (delegate* unmanaged<long, nint>)*_syscallPtr;
            return (int)syscall(SYS_gettid);
        }

        // ── Pointer validity ────────────────────────────────────────────

        // Cached /proc/self/maps snapshot for fast IsBadPtr checks.
        private static (nint start, nint end)[]? _cachedMaps;
        private static int _mapsCachedOnThread = -1;

        public override bool IsBadPtr(nint ptr)
        {
            if (ptr == 0) return true;

            int tid = GetCurrentThreadId();
            bool refresh;
            lock (_mapsLock)
            {
                refresh = _cachedMaps == null || _mapsCachedOnThread != tid;
            }

            if (refresh)
            {
                try
                {
                    var lines = File.ReadAllLines("/proc/self/maps");
                    var list = new List<(nint, nint)>(lines.Length);
                    foreach (var line in lines)
                    {
                        int dash = line.IndexOf('-');
                        int space = line.IndexOf(' ');
                        if (dash < 0 || space < 0) continue;
                        nint s = (nint)long.Parse(line.AsSpan(0, dash),
                                                   System.Globalization.NumberStyles.HexNumber);
                        nint e = (nint)long.Parse(line.AsSpan(dash + 1, space - dash - 1),
                                                   System.Globalization.NumberStyles.HexNumber);
                        list.Add((s, e));
                    }
                    lock (_mapsLock)
                    {
                        _cachedMaps = [.. list];
                        _mapsCachedOnThread = tid;
                    }
                }
                catch { return false; } // conservative: assume valid
            }

            (nint start, nint end)[] maps;
            lock (_mapsLock) { maps = _cachedMaps!; }

            foreach (var (s, e) in maps)
            {
                if (ptr >= s && ptr < e) return false;
            }
            return true;
        }

        // ── HL boot data extraction ─────────────────────────────────────
        //
        //  On Linux hlboot.dat is typically baked into the ELF via objcopy
        //  as a raw binary section, or shipped as a loose file next to the
        //  executable.  We try both.

        public override ReadOnlySpan<byte> GetHlbootDataFromExe(string exePath)
        {
            // 1) Try a loose hlboot.dat next to the executable.
            string dir = Path.GetDirectoryName(exePath) ?? ".";
            string datPath = Path.Combine(dir, "hlboot.dat");
            if (File.Exists(datPath))
            {
                return File.ReadAllBytes(datPath);
            }

            // 2) Try to find it via a known ELF section name.
            //    Hashlink's build pipeline often uses ".hlboot" or
            //    ".rodata" with a known magic prefix.
            try
            {
                using var fs = new FileStream(exePath, FileMode.Open,
                                              FileAccess.Read, FileShare.Read);
                // Look for the Hashlink bytecode magic "HLB" at offset 0
                // of an embedded blob — scan the whole binary.
                byte[] buf = new byte[fs.Length];
                fs.ReadExactly(buf);

                // The Hashlink bytecode starts with the magic bytes "HLB".
                // Search backward from the end (common for appended data).
                var span = buf.AsSpan();
                for (int i = span.Length - 4; i >= 0; i--)
                {
                    if (span[i] == 'H' && span[i + 1] == 'L' && span[i + 2] == 'B')
                    {
                        // Found — return the slice from here to end.
                        return span[i..].ToArray();
                    }
                }
            }
            catch { }

            return default;
        }

        // ── ASM: TLS data pointer helper ────────────────────────────────
        //
        //  On Linux there is no simple "mov reg, fs:[offset]" for our
        //  custom TLS, because our data lives behind pthread_key_create.
        //  Instead we call pthread_getspecific(_pthreadKey) and add the
        //  field offset.
        //
        //  Callee-saved registers (rbx,rbp,r12-r15) are untouched.
        //  We also preserve rcx for compatibility with call-site
        //  expectations carried over from the Windows implementation.

        protected override void AsmGetTlsDataPtrRax<T>(Assembler c, ref T offset)
        {
            var ofs = (nint)Unsafe.AsPointer(ref offset) - (nint)tls_template;

            // Save caller-saved registers that the call to pthread_getspecific
            // may clobber (System V: rax,rcx,rdx,rsi,rdi,r8-r11).
            // r12 is callee-saved AND we use it as a temporary rsp holder,
            // so push/pop it too to preserve the caller's value.
            c.push(r12);
            c.push(rcx);
            c.push(rdx);
            c.push(rsi);
            c.push(rdi);

            // Force 16-byte stack alignment regardless of entry rsp.
            // Save the pre-alignment rsp in r12 (CALLEE-SAVED — survives
            // the call to pthread_getspecific).  Do NOT use a caller-saved
            // register (r8–r11) here — they get clobbered by the call.
            c.mov(r12, rsp);
            c.and(rsp, -16);

            // edi  = pthread key (first integer arg in System V ABI)
            c.mov(rax, (long)_pthreadKeyPtr);
            c.mov(edi, __[rax]);

            // rax  = pthread_getspecific
            c.mov(rax, (long)_pthreadGetspecificPtr);
            c.mov(rax, __[rax]);

            // rax  = pthread_getspecific(key)  →  TlsData base pointer
            c.call(rax);

            // Restore original (possibly misaligned) rsp from callee-saved r12.
            c.mov(rsp, r12);

            // rax  = &TlsData->field
            c.lea(rax, __[rax + (int)ofs]);

            // Restore caller-saved registers.
            c.pop(rdi);
            c.pop(rsi);
            c.pop(rdx);
            c.pop(rcx);
            c.pop(r12);
        }

        // ── ASM: cs → hl context store ──────────────────────────────────
        //
        //  Platform-independent: saves the managed execution context into a
        //  register-store buffer and transfers control to Hashlink code.
        //  Same register save set as Windows (callee-saved on both ABIs).

        protected override void Generate_asm_cs_hl_store_context(Assembler c)
        {
            c.pop(r11);               // Data table pointer (pushed by caller)
            c.mov(r10, __[rsp]);      // Return IP

            c.mov(rax, rsp);          // Original stack pointer

            c.mov(rsp, __[r11]);      // Switch to register store buffer

            c.push(r10);              // Save return IP
            c.mov(r10, STACK_CHUCK_SUM);
            c.push(r10);              // Checksum

            c.push(rax);              // Save original RSP

            c.push(r10);              // Checksum

            c.push(rbx);
            c.push(rbp);
            c.push(rdi);
            c.push(rsi);
            c.push(r12);
            c.push(r13);
            c.push(r14);
            c.push(r15);

            c.push(r10);              // Checksum

            c.mov(__[r11 + 16], rsp); // Save register store position

            c.mov(rsp, rax);          // Restore original stack

            c.jmp(__qword_ptr[r11 + 8]); // Jump to target
        }

        // ── ASM: hl → cs return-pointer store ───────────────────────────
        //
        //  Platform-independent except for AsmGetTlsDataPtrRax.
        //  Hijacks the return address so Hashlink returns into managed code.

        protected override void Generate_asm_hl2cs_store_return_ptr(Assembler c)
        {
            AsmGetTlsDataPtrRax(c, ref tls_template->hl2cs_return_pointers);

            c.cmp(rax, 0x1000);       // Tls is null?
            c.jl(c.F);

            c.mov(r11, __[rax]);
            c.cmp(r11, 0);            // buffer pointer is null?
            c.je(c.F);

            c.mov(r10, __[r11]);      // full or overflow flag
            c.cmp(r10, 1);
            c.je(c.F);

            c.lea(r10, __[rsp + 8]);
            c.mov(__[r11], r10);      // store return address

            c.add(r11, 8);
            c.mov(__[rax], r11);      // advance buffer pointer

            c.AnonymousLabel();

            c.pop(rax);
            c.jmp(__qword_ptr[rax]);
        }

        // ── ASM: hl → cs exception throw ────────────────────────────────
        //
        //  Platform-independent except for AsmGetTlsDataPtrRax.

        protected override void Generate_asm_hl2cs_throw_exception(Assembler c)
        {
            AsmGetTlsDataPtrRax(c, ref tls_template->prev_hl_error_ptr);

            c.mov(rcx, __[rax]);

            c.sub(rsp, 56);

            AsmGetTlsDataPtrRax(c, ref tls_template->hl_throw_ptr);

            c.jmp(__qword_ptr[rax]);
        }

        // ── ASM: hook break_on_trap entry ───────────────────────────────
        //
        //  Intercepts Hashlink's break_on_trap.  On System V the first six
        //  integer arguments arrive in rdi,rsi,rdx,rcx,r8,r9 (vs rcx,rdx,
        //  r8,r9 on MS x64).  We save all six before calling the managed
        //  trap_filter callback.
        //
        //  The "restore execution context" tail is identical to Windows
        //  because the register-save format (cs_hl_store_context) uses the
        //  intersection of callee-saved registers on both ABIs.

        protected override void Generate_asm_hook_break_on_trap_Entry(Assembler c)
        {
            var fallback = c.CreateLabel();

            // ── Save argument registers (System V AMD64 ABI) ────────
            c.push(rdi);              // arg1: t
            c.push(rsi);              // arg2: ctx
            c.push(rdx);              // arg3: v
            c.push(rcx);              // arg4
            c.push(r8);               // arg5
            c.push(r9);               // arg6

            // Align stack to 16 bytes before call.
            // Entry rsp ≡ 8 (mod 16).  Six pushes = -48 → rsp ≡ 8 (mod 16).
            // sub 8 → rsp ≡ 0 (mod 16) for the call.
            c.sub(rsp, 8);

            c.mov(rax, (long)&Data->orig_break_on_trap);
            c.mov(r11, __[rax]);
            c.call(r11);

            c.add(rsp, 8);

            // Restore argument registers in reverse order.
            c.pop(r9);
            c.pop(r8);
            c.pop(rcx);
            c.pop(rdx);
            c.pop(rsi);
            c.pop(rdi);

            c.sub(rsp, 8);

            // Call trap_filter(t, ctx, v)
            // Arguments are already in rdi, rsi, rdx from the original call.
            c.mov(rax, (long)&Data->trap_filter);
            c.mov(r11, __[rax]);
            c.call(r11);

            c.add(rsp, 8);

            // ── Check result ────────────────────────────────────────
            c.cmp(rax, 0xff);
            c.jl(fallback);

            // ── Restore execution context ───────────────────────────
            // This section is platform-independent — it restores from the
            // format written by Generate_asm_cs_hl_store_context.

            c.mov(rsp, __[rax + 16]);

            c.pop(r10);               // Checksum
            c.cmp(r10, STACK_CHUCK_SUM);
            Assert(c);

            c.pop(r15);
            c.pop(r14);
            c.pop(r13);
            c.pop(r12);
            c.pop(rsi);
            c.pop(rdi);
            c.pop(rbp);
            c.pop(rbx);

            c.pop(r10);               // Checksum
            c.cmp(r10, STACK_CHUCK_SUM);
            Assert(c);

            c.pop(rax);

            c.pop(r10);               // Checksum
            c.cmp(r10, STACK_CHUCK_SUM);
            Assert(c);

            c.pop(r11);               // Saved return IP

            c.mov(rsp, rax);

            c.mov(rax, (long)&Data->return_from_managed);
            c.mov(__[rsp], r11);      // Fix return pointer
            c.jmp(__qword_ptr[rax]);

            c.Label(ref fallback);
            c.mov(rax, 0);
            c.ret();

            c.AnonymousLabel();
            c.int3();
        }
    }
}
