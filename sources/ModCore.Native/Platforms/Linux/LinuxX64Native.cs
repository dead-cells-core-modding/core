using Hashlink;
using ModCore.Storage;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using R = ModCore.Native.AsmAssembler.R;

namespace ModCore.Native
{
    [SupportedOSPlatform("linux")]
    internal unsafe partial class LinuxX64Native : Native
    {
        // ── mprotect constants ──────────────────────────────────────────
        private const int PROT_READ  = 0x1;
        private const int PROT_WRITE = 0x2;
        private const int PROT_EXEC  = 0x4;
        private const int PROT_NONE  = 0x0;

        // ── syscall numbers (x86_64) ────────────────────────────────────
        private const long SYS_gettid = 186;

        // ── LibraryImport declarations ──────────────────────────────────
        //  Replace explicit NativeLibrary.GetExport + delegate* calls with
        //  source-generated P/Invoke thunks (.NET 7+).

        [LibraryImport("libc.so")]
        private static partial int mprotect(nint addr, nuint len, int prot);

        [LibraryImport("libc.so")]
        private static partial nint pthread_self();

        [LibraryImport("libc.so")]
        private static partial int pthread_key_create(int* key, nint destructor);

        [LibraryImport("libc.so")]
        private static partial int pthread_setspecific(int key, nint value);

        [LibraryImport("libc.so")]
        private static partial int pthread_getattr_np(nint* thread, nint* attr);

        [LibraryImport("libc.so")]
        private static partial int pthread_attr_getstack(nint attr, nint* stackaddr, nuint* stacksize);

        [LibraryImport("libc.so")]
        private static partial int pthread_attr_destroy(nint attr);

        // ── Statically-resolved native symbols ──────────────────────────
        // Stored via NativeMemory.Alloc so generated asm can reference them
        // by absolute address without fighting .NET static-field indirection.
        private static readonly nint* _pthreadGetspecificPtr;
        private static readonly int*  _pthreadKeyPtr;
        private static readonly nint* _syscallPtr;

        // Dedicated lock objects (pointers aren't reference types in C#).
        private static readonly Lock _keyLock  = new();
        private static readonly Lock _mapsLock = new();

        static LinuxX64Native()
        {
            if (!NativeLibrary.TryLoad("libc.so", out nint libc))
                libc = NativeLibrary.Load("libc.so.6");

            _pthreadGetspecificPtr = (nint*)NativeMemory.Alloc((nuint)sizeof(nint));
            *_pthreadGetspecificPtr = NativeLibrary.GetExport(libc, "pthread_getspecific");

            _syscallPtr = (nint*)NativeMemory.Alloc((nuint)sizeof(nint));
            *_syscallPtr = NativeLibrary.GetExport(libc, "syscall");

            _pthreadKeyPtr = (int*)NativeMemory.Alloc((nuint)sizeof(int));
            *_pthreadKeyPtr = -1;
        }

        // ── Library loading ─────────────────────────────────────────────

        public override bool TryLoadLibrary(string path, out nint handle)
        {
            if("libhl".Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
            {
                if (NativeLibrary.TryLoad(path + ".so", out handle))
                    return true;
            }
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

                int key = 0;
                int rc = pthread_key_create(&key, 0);
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
            pthread_setspecific(index, val);
        }

        // ── Memory page protection ──────────────────────────────────────
        //  Cache the original protection per page so RestorePageProtect
        //  can put it back.  mprotect() itself does not return the old
        //  value, so we derive a reasonable default when the cache misses.

        private static readonly Dictionary<nint, int> _pageProtCache = [];

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

            mprotect(pageStart, (nuint)Environment.SystemPageSize,
                     PROT_READ | PROT_WRITE | PROT_EXEC);
        }

        public override void RestorePageProtect(nint ptr, int val)
        {
            nint pageStart = ptr & ~(Environment.SystemPageSize - 1);
            mprotect(pageStart, (nuint)Environment.SystemPageSize, val);

            lock (_pageProtCache)
            {
                _pageProtCache[pageStart] = val;
            }
        }

        // ── Stack helpers ───────────────────────────────────────────────

        public override nint GetCurrentThreadStackBase()
        {
            return 0;
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
        protected static int GetCurrentThreadId()
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
        //  r12 is used as a temporary rsp holder across the call and is
        //  preserved (push/pop) for the caller.
        //  rcx is preserved for compatibility with call-site expectations
        //  carried over from the Windows implementation.

        protected override void AsmGetTlsDataPtrRax<T>(AsmAssembler c, ref T offset)
        {
            var ofs = (nint)Unsafe.AsPointer(ref offset) - (nint)tls_template;

            // Save caller-saved registers that the call to pthread_getspecific
            // may clobber (System V: rax,rcx,rdx,rsi,rdi,r8-r11).
            // r12 is callee-saved AND we use it as a temporary rsp holder,
            // so push/pop it too to preserve the caller's value.
            // ── pushq %r12; pushq %rcx; pushq %rdx; pushq %rsi; pushq %rdi
            c.push(R.r12);
            c.push(R.rcx);
            c.push(R.rdx);
            c.push(R.rsi);
            c.push(R.rdi);

            // Force 16-byte stack alignment regardless of entry rsp.
            // Save the pre-alignment rsp in r12 (CALLEE-SAVED — survives
            // the call to pthread_getspecific).  Do NOT use a caller-saved
            // register (r8–r11) here — they get clobbered by the call.
            // ── movq %rsp, %r12
            c.mov_rr(R.r12, R.rsp);
            // ── andq $-16, %rsp
            c.and(R.rsp, -16);

            // edi  = pthread key (first integer arg in System V ABI)
            // ── movabsq $_pthreadKeyPtr, %rax
            c.mov_imm(R.rax, (long)_pthreadKeyPtr);
            // ── movl (%rax), %edi
            c.AddLine("movl (%rax), %edi");

            // rax  = pthread_getspecific
            // ── movabsq $_pthreadGetspecificPtr, %rax; movq (%rax), %rax
            c.mov_imm(R.rax, (long)_pthreadGetspecificPtr);
            c.mov_mr(R.rax, R.rax);

            // rax  = pthread_getspecific(key)  →  TlsData base pointer
            // ── callq *%rax
            c.call_r(R.rax);

            // Restore original (possibly misaligned) rsp from callee-saved r12.
            // ── movq %r12, %rsp
            c.mov_rr(R.rsp, R.r12);

            // rax  = &TlsData->field
            // ── leaq ofs(%rax), %rax
            c.lea(R.rax, R.rax, (int)ofs);

            // Restore caller-saved registers in reverse order.
            // ── popq %rdi; popq %rsi; popq %rdx; popq %rcx; popq %r12
            c.pop(R.rdi);
            c.pop(R.rsi);
            c.pop(R.rdx);
            c.pop(R.rcx);
            c.pop(R.r12);
        }

        // ── ASM: cs → hl context store ──────────────────────────────────
        //
        //  Platform-independent: saves the managed execution context into a
        //  register-store buffer and transfers control to Hashlink code.
        //  Same register save set as Windows (callee-saved on both ABIs).

        protected override void Generate_asm_cs_hl_store_context(AsmAssembler c)
        {
            // pop r11   (Data table pointer pushed by caller)
            c.pop(R.r11);

            // mov r10, [rsp]   →   movq (%rsp), %r10   (Return IP)
            c.mov_mr(R.r10, R.rsp);

            // mov rax, rsp   →   movq %rsp, %rax   (Original stack pointer)
            c.mov_rr(R.rax, R.rsp);

            // mov rsp, [r11]   →   movq (%r11), %rsp   (Switch to register store buffer)
            c.mov_mr(R.rsp, R.r11);

            // push r10   (Save return IP)
            c.push(R.r10);

            // mov r10, STACK_CHUCK_SUM; push r10   (Checksum)
            c.mov_imm(R.r10, STACK_CHUCK_SUM);
            c.push(R.r10);

            // push rax   (Save original RSP)
            c.push(R.rax);

            // push r10   (Checksum)
            c.push(R.r10);

            c.push(R.rbx);
            c.push(R.rbp);
            c.push(R.rdi);
            c.push(R.rsi);
            c.push(R.r12);
            c.push(R.r13);
            c.push(R.r14);
            c.push(R.r15);

            // push r10   (Checksum)
            c.push(R.r10);

            // mov [r11 + 16], rsp   →   movq %rsp, 16(%r11)   (Save register store position)
            c.mov_rm(R.rsp, R.r11, 16);

            // mov rsp, rax   →   movq %rax, %rsp   (Restore original stack)
            c.mov_rr(R.rsp, R.rax);

            // jmp qword ptr [r11 + 8]   →   jmpq *8(%r11)   (Jump to target)
            c.jmp_m(R.r11, 8);
        }

        // ── ASM: hl → cs return-pointer store ───────────────────────────
        //
        //  Platform-independent except for AsmGetTlsDataPtrRax.
        //  Hijacks the return address so Hashlink returns into managed code.

        protected override void Generate_asm_hl2cs_store_return_ptr(AsmAssembler c)
        {
            AsmGetTlsDataPtrRax(c, ref tls_template->hl2cs_return_pointers);

            // cmp rax, 0x1000   →   cmpq $0x1000, %rax   (Tls is null?)
            c.cmp_ri(R.rax, 0x1000);
            c.jl(c.F);

            // mov r11, [rax]   →   movq (%rax), %r11
            c.mov_mr(R.r11, R.rax);

            // cmp r11, 0   →   cmpq $0, %r11   (buffer pointer is null?)
            c.cmp_ri(R.r11, 0);
            c.je(c.F);

            // mov r10, [r11]   →   movq (%r11), %r10   (full or overflow flag)
            c.mov_mr(R.r10, R.r11);

            // cmp r10, 1
            c.cmp_ri(R.r10, 1);
            c.je(c.F);

            // lea r10, [rsp + 8]   →   leaq 8(%rsp), %r10
            c.lea(R.r10, R.rsp, 8);

            // mov [r11], r10   →   movq %r10, (%r11)   (store return address)
            c.mov_rm(R.r10, R.r11);

            // add r11, 8
            c.add(R.r11, 8);

            // mov [rax], r11   →   movq %r11, (%rax)   (advance buffer pointer)
            c.mov_rm(R.r11, R.rax);

            c.AnonymousLabel();

            // pop rax; jmp qword ptr [rax]
            c.pop(R.rax);
            c.jmp_m(R.rax);
        }

        // ── ASM: hl → cs exception throw ────────────────────────────────
        //
        //  Uses rdi for the first argument (System V ABI) instead of rcx
        //  (MS x64 ABI).  Otherwise platform-independent.

        protected override void Generate_asm_hl2cs_throw_exception(AsmAssembler c)
        {
            AsmGetTlsDataPtrRax(c, ref tls_template->prev_hl_error_ptr);

            // mov rdi, [rax]   →   movq (%rax), %rdi   (first arg in System V ABI)
            c.mov_mr(R.rdi, R.rax);

            // sub rsp, 56   (align stack before jump to throw handler)
            c.sub(R.rsp, 56);

            AsmGetTlsDataPtrRax(c, ref tls_template->hl_throw_ptr);

            // jmp qword ptr [rax]   →   jmpq *(%rax)
            c.jmp_m(R.rax);
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

        protected override void Generate_asm_hook_break_on_trap_Entry(AsmAssembler c)
        {
            var fallback = c.CreateLabel();

            // ── Save argument registers (System V AMD64 ABI) ─────────
            // pushq %rdi; pushq %rsi; pushq %rdx; pushq %rcx; pushq %r8; pushq %r9
            c.push(R.rdi);              // arg1: t
            c.push(R.rsi);              // arg2: ctx
            c.push(R.rdx);              // arg3: v
            c.push(R.rcx);              // arg4
            c.push(R.r8);               // arg5
            c.push(R.r9);               // arg6

            // Align stack to 16 bytes before call.
            // Entry rsp ≡ 8 (mod 16).  Six pushes = -48 → rsp ≡ 8 (mod 16).
            // sub 8 → rsp ≡ 0 (mod 16) for the call.
            c.sub(R.rsp, 8);

            // Call orig_break_on_trap
            c.mov_imm(R.rax, (long)&Data->orig_break_on_trap);
            c.mov_mr(R.r11, R.rax);
            c.call_r(R.r11);

            c.add(R.rsp, 8);

            // Restore argument registers in reverse order.
            // popq %r9; popq %r8; popq %rcx; popq %rdx; popq %rsi; popq %rdi
            c.pop(R.r9);
            c.pop(R.r8);
            c.pop(R.rcx);
            c.pop(R.rdx);
            c.pop(R.rsi);
            c.pop(R.rdi);

            // Align + call trap_filter(t, ctx, v)
            // Arguments are already in rdi, rsi, rdx from the original call.
            c.sub(R.rsp, 8);

            c.mov_imm(R.rax, (long)&Data->trap_filter);
            c.mov_mr(R.r11, R.rax);
            c.call_r(R.r11);

            c.add(R.rsp, 8);

            // ── Check result ─────────────────────────────────────────
            // cmp rax, 0xff
            c.cmp_ri(R.rax, 0xff);
            c.jl(fallback);

            // ── Restore execution context ────────────────────────────
            // This section is platform-independent — it restores from the
            // format written by Generate_asm_cs_hl_store_context.

            // mov rsp, [rax + 16]   →   movq 16(%rax), %rsp
            c.mov_mr(R.rsp, R.rax, 16);

            // pop r10   (Checksum)
            c.pop(R.r10);
            c.cmp_ri(R.r10, STACK_CHUCK_SUM);
            Assert(c);

            c.pop(R.r15);
            c.pop(R.r14);
            c.pop(R.r13);
            c.pop(R.r12);
            c.pop(R.rsi);
            c.pop(R.rdi);
            c.pop(R.rbp);
            c.pop(R.rbx);

            // pop r10   (Checksum)
            c.pop(R.r10);
            c.cmp_ri(R.r10, STACK_CHUCK_SUM);
            Assert(c);

            c.pop(R.rax);

            // pop r10   (Checksum)
            c.pop(R.r10);
            c.cmp_ri(R.r10, STACK_CHUCK_SUM);
            Assert(c);

            // pop r11   (Saved return IP)
            c.pop(R.r11);

            // mov rsp, rax   →   movq %rax, %rsp
            c.mov_rr(R.rsp, R.rax);

            // mov rax, &Data->return_from_managed
            c.mov_imm(R.rax, (long)&Data->return_from_managed);

            // mov [rsp], r11   →   movq %r11, (%rsp)   (Fix return pointer)
            c.mov_rm(R.r11, R.rsp);

            // jmp qword ptr [rax]   →   jmpq *(%rax)
            c.jmp_m(R.rax);

            // ── Fallback ─────────────────────────────────────────────
            c.Label(ref fallback);
            c.mov_imm(R.rax, 0);
            c.ret();

            // Unreachable tail — breakpoint guard
            c.AnonymousLabel();
            c.int3();
        }

        // ── ASM: custom longjmp ──────────────────────────────────────────
        //
        //  Stub on Linux — the HLC longjmp path is not currently used.
        //  A real implementation would need to comply with System V AMD64 ABI
        //  (first two args in rdi, rsi).

        protected override void Generate_asm_custom_longjump(AsmAssembler c)
        {
            c.int3();
        }
    }
}
