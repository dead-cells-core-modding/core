using Hashlink;
using ModCore.Storage;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ModCore.Native
{
    [SupportedOSPlatform("linux")]
    internal unsafe partial class LinuxArm64Native : Native
    {
        // ── mprotect constants ──────────────────────────────────────────
        private const int PROT_READ = 0x1;
        private const int PROT_WRITE = 0x2;
        private const int PROT_EXEC = 0x4;
        private const int PROT_NONE = 0x0;

        // ── AArch64 Linux syscall numbers ───────────────────────────────
        private const long SYS_gettid = 178;
        private const long SYS_mprotect = 226;

        // ── LibraryImport declarations ──────────────────────────────────

        [LibraryImport("libc.so")]
        private static partial int mprotect(nint addr, nuint len, int prot);

        [LibraryImport("libc.so")]
        private static partial int pthread_key_create(int* key, nint destructor);

        [LibraryImport("libc.so")]
        private static partial int pthread_setspecific(int key, nint value);

        [LibraryImport("libc.so")]
        private static partial int pthread_getattr_np(nint thread, nint* attr);

        [LibraryImport("libc.so")]
        private static partial int pthread_attr_getstack(nint attr, nint* stackaddr, nuint* stacksize);

        [LibraryImport("libc.so")]
        private static partial int pthread_attr_destroy(nint attr);

        [LibraryImport("libc.so", StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint dlopen(string path, int mode);

        [LibraryImport("libc.so")]
        private static partial int dlclose(nint handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct Dl_info
        {
            public nint dli_fname;
            public nint dli_fbase;
            public nint dli_sname;
            public nint dli_saddr;
        }

        [LibraryImport("libc.so")]
        private static partial int dladdr(nint addr, Dl_info* info);

        [LibraryImport("libc.so", StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint dlsym(nint handle, string symbol);

        // ── Statically-resolved native symbols ──────────────────────────
        //  Stored via NativeMemory.Alloc so generated asm can reference them
        //  by absolute address without fighting .NET static-field indirection.
        private static readonly nint* _pthreadGetspecificPtr;
        private static readonly int* _pthreadKeyPtr;
        private static readonly nint* _syscallPtr;

        // Dedicated lock objects (pointers aren't reference types in C#).
        private static readonly Lock _keyLock = new();
        private static readonly Lock _mapsLock = new();

        static LinuxArm64Native()
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
            if ("libhl".Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
            {
                if (NativeLibrary.TryLoad(path + ".so.1", out handle))
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

        /// <summary>
        /// Override <see cref="Native.LoadLibrary"/> to use dlopen so that
        /// the returned handle is a glibc link_map*, from which
        /// <see cref="GetModuleBaseAddress"/> can extract the load address.
        /// </summary>
        public override nint LoadLibrary(string path)
        {
            // RTLD_NOW = 2, RTLD_GLOBAL = 0x100
            const int RTLD_NOW = 2;
            const int RTLD_GLOBAL = 0x100;

            if ("libhl".Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
            {
                var h = dlopen(path + ".so.1", RTLD_NOW | RTLD_GLOBAL);
                if (h != 0) return h;
            }
            var handle = dlopen(path, RTLD_NOW | RTLD_GLOBAL);
            if (handle != 0) return handle;
            handle = dlopen(path + ".so", RTLD_NOW | RTLD_GLOBAL);
            if (handle != 0) return handle;
            handle = dlopen(FolderInfo.CurrentNativeRoot.GetFilePath(path), RTLD_NOW | RTLD_GLOBAL);
            if (handle != 0) return handle;
            handle = dlopen(FolderInfo.CurrentNativeRoot.GetFilePath(path + ".so"), RTLD_NOW | RTLD_GLOBAL);
            if (handle != 0) return handle;
            return 0;
        }

        /// <summary>
        /// On glibc Linux, dlopen returns a link_map*, whose first field
        /// (l_addr) is the module load base.  Fall back to dladdr probing
        /// if the direct read returns 0 (e.g. non-glibc libc).
        /// </summary>
        public override nint GetModuleBaseAddress(nint libHandle)
        {
            // glibc: first field of link_map is l_addr (the load base).
            nint baseAddr = Marshal.ReadIntPtr(libHandle);
            if (baseAddr != 0)
                return baseAddr;

            // Fallback: dladdr probe using known exports.
            foreach (var probe in _baseProbeSymbols)
            {
                var sym = dlsym(libHandle, probe);
                if (sym != 0)
                {
                    Dl_info info = default;
                    if (dladdr(sym, &info) != 0 && info.dli_fbase != 0)
                        return info.dli_fbase;
                }
            }
            throw new InvalidOperationException(
                "Unable to resolve module base address for the given handle.");
        }

        private static readonly string[] _baseProbeSymbols =
        [
            "hl_global_init", "hl_alloc_array", "hl_gc_alloc_gen",
            "hl_code_read", "hl_module_alloc"
        ];

        // ── Native hooks ─────────────────────────────────────────────────

        [UnmanagedCallersOnly]
        private static int Hook_throw_handler(int code) => 0;

        protected override void InitializeNativeHooks()
        {
            base.InitializeNativeHooks();
        }

        // ── Assembly initialization (before base.InitializeAsm) ─────────

        protected override void InitializeAsm()
        {
            // Ensure TLS key is allocated before base generates asm.
            if (*_pthreadKeyPtr < 0)
                AllocPthreadKey();

            LoadLibrary(FolderInfo.CurrentNativeRoot.GetFilePath("libtcc"));

            var assembler = new AsmAssembler();
            nativeCodePage = (nint)HashlinkNative.hl_alloc_executable_memory(8192);

            *Data = new()
            {
                tls_slot_index = AllocTls()
            };

            var st = GetType();
            var dict = new Dictionary<string, FieldInfo>();

            using var stream = new UnmanagedMemoryStream((byte*)nativeCodePage, 8192, 8192, FileAccess.ReadWrite);
            foreach (var v in st.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (v.FieldType != typeof(nint) ||
                    !v.Name.StartsWith("asm_"))
                {
                    continue;
                }
                var generator = st.GetMethod("Generate_" + v.Name, BindingFlags.Instance | BindingFlags.NonPublic);

                Debug.Assert(generator != null);

                var symName = "SYM_" + v.Name;
                assembler.DefineGlobalSymbol(symName);

                dict.Add(symName, v);

                generator.Invoke(this, [assembler]);
            }

            assembler.Compiler.OnError += Compiler_OnError;
            assembler.Compile();

            foreach ((var sym, var f) in dict)
            {
                f.SetValue(this, assembler.GetSymbol(sym));
            }
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

            // Remote thread: read /proc/self/task/<tid>/stat (field 28: kstkesp).
            // On AArch64, /proc/self/task/<tid>/stat has the same format as x86_64.
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
                int closeParen = stat.LastIndexOf(')');
                if (closeParen < 0) return 0;
                // After ") " there are 25 more fields; we want the 28th
                // overall = (28 - 2 - 1) = 25th after the closing paren.
                var tail = stat.AsSpan(closeParen + 2);
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
        /// Get the kernel TID for the calling thread via syscall(SYS_gettid).
        /// AArch64 syscall number 178.
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
        //  executable.  We try both (same strategy as LinuxX64Native).

        public override ReadOnlySpan<byte> GetHlbootDataFromExe(string exePath)
        {
            // 1) Try a loose hlboot.dat next to the executable.
            string dir = Path.GetDirectoryName(exePath) ?? ".";
            string datPath = Path.Combine(dir, "hlboot.dat");
            if (File.Exists(datPath))
            {
                return File.ReadAllBytes(datPath);
            }

            // 2) Try to find it embedded in the ELF (scan for "HLB" magic).
            try
            {
                using var fs = new FileStream(exePath, FileMode.Open,
                                              FileAccess.Read, FileShare.Read);
                byte[] buf = new byte[fs.Length];
                fs.ReadExactly(buf);

                var span = buf.AsSpan();
                for (int i = span.Length - 4; i >= 0; i--)
                {
                    if (span[i] == 'H' && span[i + 1] == 'L' && span[i + 2] == 'B')
                    {
                        return span[i..].ToArray();
                    }
                }
            }
            catch { }

            return default;
        }

        // ── Helper: load 64-bit immediate into AArch64 register ──────────
        //
        //  On AArch64 there is no single instruction for arbitrary 64-bit
        //  immediates; we use movz + up to three movk instructions.

        private static void EmitMovImm64(AsmAssembler c, string reg, long value)
        {
            ushort h0 = (ushort)(value & 0xFFFF);
            ushort h1 = (ushort)((value >> 16) & 0xFFFF);
            ushort h2 = (ushort)((value >> 32) & 0xFFFF);
            ushort h3 = (ushort)((value >> 48) & 0xFFFF);

            c.AddLine($"movz {reg}, #0x{h0:x4}");

            bool needH1 = h1 != 0 || (h2 != 0 || h3 != 0);
            bool needH2 = h2 != 0 || h3 != 0;
            bool needH3 = h3 != 0;

            // Only emit movk for halves that are non-zero, OR if a higher
            // half is non-zero (in which case we must emit an explicit 0
            // to avoid leaving stale bits in the register).
            if (needH1)
                c.AddLine($"movk {reg}, #0x{h1:x4}, lsl #16");
            if (needH2)
                c.AddLine($"movk {reg}, #0x{h2:x4}, lsl #32");
            if (needH3)
                c.AddLine($"movk {reg}, #0x{h3:x4}, lsl #48");
        }

        // ═════════════════════════════════════════════════════════════════
        //  ASM: TLS data pointer helper
        // ═════════════════════════════════════════════════════════════════
        //
        //  On AArch64, pthread_getspecific result arrives in x0 (AAPCS64
        //  integer return register — same name as "Rax" in the method
        //  name, kept for consistency with the x64 base).

        /// <summary>
        /// Inline code snippet (NOT a separate function — no ret emitted).
        /// Loads &amp;TlsData→field into x0.
        /// Saves/restores x30 (LR) around the pthread_getspecific call so
        /// that the containing function's return address is preserved.
        /// Clobbers x1,x2 (caller-saved temps); preserves all callee-saved regs.
        /// Stack alignment is maintained (stp/ldp with xzr keeps sp ≡ 0 mod 16).
        /// </summary>
        protected override void AsmGetTlsDataPtrRax<T>(AsmAssembler c, ref T offset)
        {
            var ofs = (nint)Unsafe.AsPointer(ref offset) - (nint)tls_template;

            // ── Save x30 (LR) across the upcoming blr x2 ──
            // stp with xzr keeps sp 16-byte aligned.
            c.AddLine("stp x30, xzr, [sp, #-16]!");

            // ── Load key pointer address into x1 ──
            EmitMovImm64(c, "x1", (long)_pthreadKeyPtr);
            // ── Load key value into w0 (first integer arg, zero-extends) ──
            c.AddLine("ldr w0, [x1]");

            // ── Load pthread_getspecific function pointer into x2 ──
            EmitMovImm64(c, "x2", (long)_pthreadGetspecificPtr);
            c.AddLine("ldr x2, [x2]");

            // ── Call pthread_getspecific(key) → x0 = TlsData base ──
            // (blr clobbers x30; we saved it above)
            c.AddLine("blr x2");

            // ── Restore x30 (LR) ──
            c.AddLine("ldp x30, xzr, [sp], #16");

            // ── Add field offset → x0 = &TlsData->field ──
            EmitMovImm64(c, "x1", (long)ofs);
            c.AddLine("add x0, x0, x1");
        }

        // ═════════════════════════════════════════════════════════════════
        //  ASM: cs → hl context store
        // ═════════════════════════════════════════════════════════════════
        //
        //  Saves AArch64 register context into a caller-provided buffer,
        //  then jumps to a target address.  The buffer layout matches the
        //  x64 convention so that the restore path in
        //  asm_hook_break_on_trap_Entry can use the same format.
        //
        //  On entry, x0 contains the pointer to {buffer*, target} struct
        //  (same AsmHelperData layout as x64: buffer at +0, target at +8).
        //
        //  Saved: original SP, LR, callee-saved GPRs (x19–x30), d8–d15,
        //  NZCV flags, plus checksum markers (STACK_CHUCK_SUM).

        protected override void Generate_asm_cs_hl_store_context(AsmAssembler c)
        {
            // x0 = pointer to {buffer*, target}  (data table pointer)

            // ── Save temps in caller-saved regs (x9-x15) ──
            c.AddLine("mov x9, x0");     // x9  = data table pointer
            c.AddLine("mov x10, x30");   // x10 = original LR
            c.AddLine("mov x11, sp");    // x11 = original SP

            // ── Load buffer address from [x9] and switch to it ──
            c.AddLine("ldr x12, [x9]");  // x12 = buffer pointer
            c.AddLine("mov sp, x12");

            // ── Push checksum marker ──
            EmitMovImm64(c, "x12", STACK_CHUCK_SUM);
            c.AddLine("stp xzr, x12, [sp, #-16]!");

            // ── Save original LR (x10) ──
            c.AddLine("stp xzr, x10, [sp, #-16]!");

            // ── Checksum ──
            EmitMovImm64(c, "x12", STACK_CHUCK_SUM);
            c.AddLine("stp xzr, x12, [sp, #-16]!");

            // ── Save original SP (x11) ──
            c.AddLine("stp xzr, x11, [sp, #-16]!");

            // ── Checksum ──
            EmitMovImm64(c, "x12", STACK_CHUCK_SUM);
            c.AddLine("stp xzr, x12, [sp, #-16]!");

            // ── Save callee-saved GPRs x19–x30 (12 registers, 6 stp pairs) ──
            // x19–x30 are NOT overwritten before this — their values are
            // the original managed-code callee-saved context.
            c.AddLine("stp x30, x29, [sp, #-16]!");
            c.AddLine("stp x28, x27, [sp, #-16]!");
            c.AddLine("stp x26, x25, [sp, #-16]!");
            c.AddLine("stp x24, x23, [sp, #-16]!");
            c.AddLine("stp x22, x21, [sp, #-16]!");
            c.AddLine("stp x20, x19, [sp, #-16]!");

            // ── Save NEON callee-saved d8–d15 (8 × 8 bytes, 4 stp pairs) ──
            c.AddLine("stp d15, d14, [sp, #-16]!");
            c.AddLine("stp d13, d12, [sp, #-16]!");
            c.AddLine("stp d11, d10, [sp, #-16]!");
            c.AddLine("stp d9, d8, [sp, #-16]!");

            // ── Save NZCV flags ──
            c.AddLine("mrs x12, nzcv");
            c.AddLine("stp xzr, x12, [sp, #-16]!");

            // ── Checksum ──
            EmitMovImm64(c, "x12", STACK_CHUCK_SUM);
            c.AddLine("stp xzr, x12, [sp, #-16]!");

            // ── Save register store position at [x9 + 16] ──
            c.AddLine("str sp, [x9, #16]");

            // ── Restore original SP ──
            c.AddLine("mov sp, x11");

            // ── Jump to target at [x9 + 8] ──
            c.AddLine("ldr x12, [x9, #8]");
            c.AddLine("br x12");
        }

        // ═════════════════════════════════════════════════════════════════
        //  ASM: hl → cs return-pointer store
        // ═════════════════════════════════════════════════════════════════
        //
        //  Hijacks the return address so Hashlink returns into managed code.
        //  On AArch64, the caller's return address is in x30 (LR) on entry,
        //  so we store it into the TLS return-pointer buffer, then ret
        //  normally (the buffer consumer will later redirect to us).

        protected override void Generate_asm_hl2cs_store_return_ptr(AsmAssembler c)
        {
            // Get TLS data pointer → x0 = &tls_template->hl2cs_return_pointers
            AsmGetTlsDataPtrRax(c, ref tls_template->hl2cs_return_pointers);

            // ── cmp x0, #0x1000 (Tls is null?) ──
            c.AddLine("cmp x0, #0x1000");
            c.AddLine($"b.lt {c.F}");

            // ── Load buffer pointer from [x0] into x1 ──
            c.AddLine("ldr x1, [x0]");

            // ── cmp x1, #0 (buffer pointer is null?) ──
            c.AddLine("cmp x1, #0");
            c.AddLine($"b.eq {c.F}");

            // ── Load value at [x1] into x2 (full/overflow flag) ──
            c.AddLine("ldr x2, [x1]");

            // ── cmp x2, #1 (overflow?) ──
            c.AddLine("cmp x2, #1");
            c.AddLine($"b.eq {c.F}");

            // ── Store LR (x30 = return address) to [x1] ──
            c.AddLine("str x30, [x1]");

            // ── Advance buffer pointer: x1 += 8 ──
            c.AddLine("add x1, x1, #8");

            // ── Write back advanced pointer to [x0] ──
            c.AddLine("str x1, [x0]");

            c.AnonymousLabel();

            // ── Return normally (LR still holds original return address) ──
            c.AddLine("ret");
        }

        // ═════════════════════════════════════════════════════════════════
        //  ASM: hl → cs exception throw
        // ═════════════════════════════════════════════════════════════════
        //
        //  Loads the exception pointer into x0 (first arg in AAPCS64)
        //  and jumps to the Hashlink throw handler.  Callee-saved
        //  registers x19–x28 are preserved across the jump.
        //
        //  This is called as the return target of a hijacked frame — LR
        //  is already the Hashlink throw handler address.

        protected override void Generate_asm_hl2cs_throw_exception(AsmAssembler c)
        {
            // Get TLS data pointer → x0 = &tls_template->prev_hl_error_ptr
            AsmGetTlsDataPtrRax(c, ref tls_template->prev_hl_error_ptr);

            // ── Load exception pointer into x0 (first arg in AAPCS64) ──
            c.AddLine("ldr x0, [x0]");

            // ── Save exception ptr in x19 (callee-saved) ──
            c.AddLine("mov x19, x0");

            // Get TLS data pointer → x0 = &tls_template->hl_throw_ptr
            AsmGetTlsDataPtrRax(c, ref tls_template->hl_throw_ptr);

            // ── Load jump target from [x0] into x1 ──
            c.AddLine("ldr x1, [x0]");

            // ── Restore x0 = exception pointer ──
            c.AddLine("mov x0, x19");

            // ── Jump to throw handler (x1 = target address) ──
            c.AddLine("br x1");
        }

        // ═════════════════════════════════════════════════════════════════
        //  ASM: hook break_on_trap entry
        // ═════════════════════════════════════════════════════════════════
        //
        //  Intercepts Hashlink's break_on_trap.  On AAPCS64 the first
        //  eight integer arguments arrive in x0–x7:
        //    x0 = t,  x1 = ctx,  x2 = v
        //
        //  Saves argument registers, calls orig_break_on_trap, then
        //  trap_filter(t, ctx, v).  If trap_filter returns >= 0xff,
        //  restores the execution context saved by cs_hl_store_context
        //  and jumps to return_from_managed.

        protected override void Generate_asm_hook_break_on_trap_Entry(AsmAssembler c)
        {
            var fallback = c.CreateLabel();

            // ── Save argument registers and frame ─────────────────────
            // Entry sp ≡ 0 mod 16 (AAPCS64).  We need to preserve
            // x0-x7 (caller-saved args), and align for calls.

            // Save x0-x7 onto stack (8 * 8 = 64 bytes)
            c.AddLine("stp x29, x30, [sp, #-16]!");
            c.AddLine("stp x6, x7, [sp, #-16]!");
            c.AddLine("stp x4, x5, [sp, #-16]!");
            c.AddLine("stp x2, x3, [sp, #-16]!");
            c.AddLine("stp x0, x1, [sp, #-16]!");

            // Stack now: 5 stp = 80 bytes below original sp.
            // sp ≡ 0 mod 16 after 5 × 16 = 80 bytes (80 ≡ 0 mod 16). Good.

            // ── Call orig_break_on_trap ─────────────────────────────
            // Use x9 as temp to avoid clobbering x0-x2 (the arguments)
            EmitMovImm64(c, "x9", (long)&Data->orig_break_on_trap);
            c.AddLine("ldr x16, [x9]");
            c.AddLine("blr x16");

            // ── Restore argument registers ──────────────────────────
            c.AddLine("ldp x0, x1, [sp], #16");
            c.AddLine("ldp x2, x3, [sp], #16");
            c.AddLine("ldp x4, x5, [sp], #16");
            c.AddLine("ldp x6, x7, [sp], #16");

            // ── Call trap_filter(x0, x1, x2) ───────────────────────
            // Args already in x0, x1, x2 from the restore above.
            // sp now points to saved x29,x30 (16-byte aligned).

            EmitMovImm64(c, "x16", (long)&Data->trap_filter);
            c.AddLine("ldr x16, [x16]");
            c.AddLine("blr x16");

            // ── Check result ─────────────────────────────────────────
            // cmp x0, #0xff
            c.AddLine("cmp x0, #0xff");
            c.AddLine($"b.lt {fallback}");

            // ── Restore execution context ────────────────────────────
            // This section restores from the format written by
            // Generate_asm_cs_hl_store_context.
            //
            // x0 = trap_filter result → points to the context buffer
            // (the `buffer` field at offset 16 of the AsmHelperData struct,
            //  which holds the register store position).

            // ── Load register store position from [x0 + 16] into sp ──
            c.AddLine("ldr sp, [x0, #16]");

            // ── Pop checksum (top of context stack) ──
            c.AddLine("ldp xzr, x10, [sp], #16");
            EmitMovImm64(c, "x11", STACK_CHUCK_SUM);
            c.AddLine("cmp x10, x11");
            A64Assert(c);

            // ── Pop NZCV ──
            c.AddLine("ldp xzr, x10, [sp], #16");
            c.AddLine("msr nzcv, x10");

            // ── Pop d8–d15 (NEON) ──
            c.AddLine("ldp d8, d9, [sp], #16");
            c.AddLine("ldp d10, d11, [sp], #16");
            c.AddLine("ldp d12, d13, [sp], #16");
            c.AddLine("ldp d14, d15, [sp], #16");

            // ── Pop callee-saved GPRs x19–x30 ──
            c.AddLine("ldp x19, x20, [sp], #16");
            c.AddLine("ldp x21, x22, [sp], #16");
            c.AddLine("ldp x23, x24, [sp], #16");
            c.AddLine("ldp x25, x26, [sp], #16");
            c.AddLine("ldp x27, x28, [sp], #16");
            c.AddLine("ldp x29, x30, [sp], #16");

            // ── Pop checksum ──
            c.AddLine("ldp xzr, x10, [sp], #16");
            EmitMovImm64(c, "x11", STACK_CHUCK_SUM);
            c.AddLine("cmp x10, x11");
            A64Assert(c);

            // ── Pop original SP (written as x21 during save) ──
            c.AddLine("ldp xzr, x10, [sp], #16");
            // x10 = original SP

            // ── Pop checksum ──
            c.AddLine("ldp xzr, x11, [sp], #16");
            EmitMovImm64(c, "x12", STACK_CHUCK_SUM);
            c.AddLine("cmp x11, x12");
            A64Assert(c);

            // ── Pop saved LR (x20 during save) ──
            c.AddLine("ldp xzr, x11, [sp], #16");
            // x11 = original LR (saved as x20)

            // ── Pop checksum ──
            c.AddLine("ldp xzr, x12, [sp], #16");
            EmitMovImm64(c, "x13", STACK_CHUCK_SUM);
            c.AddLine("cmp x12, x13");
            A64Assert(c);

            // ── Restore original SP ──
            c.AddLine("mov sp, x10");

            // ── Load return_from_managed target ──
            EmitMovImm64(c, "x9", (long)&Data->return_from_managed);
            c.AddLine("ldr x16, [x9]");

            // ── Set LR to saved return address ──
            // (On ARM64 ret uses x30, unlike x64 which pops from stack)
            c.AddLine("mov x30, x11");

            // ── Jump to return_from_managed ──
            c.AddLine("br x16");

            // ── Fallback ────────────────────────────────────────────
            c.Label(ref fallback);
            c.AddLine("mov x0, #0");
            c.AddLine("ret");

            // Unreachable tail — breakpoint guard
            c.AnonymousLabel();
            c.AddLine("brk #0");
        }

        // ═════════════════════════════════════════════════════════════════
        //  ASM: custom longjmp
        // ═════════════════════════════════════════════════════════════════
        //
        //  On AArch64, restores register context from HL_trap_ctx and
        //  performs a long jump.  The context (saved by cs_hl_store_context)
        //  contains GPRs, NEON regs, NZCV, SP, and LR.
        //
        //  Entry: x0 = pointer to HL_trap_ctx (context to restore)
        //         x1 = return value to place in x0 before jump

        protected override void Generate_asm_custom_longjump(AsmAssembler c)
        {
            // x0 = context pointer, x1 = return value
            // Save return value in x13 (caller-saved, NOT part of context restore).
            c.AddLine("mov x13, x1");

            // ── context points past C_jmpbuf (skipping the setjmp buffer) ──
            // The actual saved context starts at offset sizeof(C_jmpbuf).
            // On glibc AArch64, C_jmpbuf ≈ 296 bytes, but we use the same
            // convention as x64: context = &trap_ctx.buf + sizeof(C_jmpbuf).
            // However, since our cs_hl_store_context saves to a custom buffer
            // (not directly into HL_trap_ctx), the longjmp restores from
            // the same custom buffer format.
            //
            // The buffer layout (from cs_hl_store_context):
            //   [checksum] [LR] [checksum] [SP] [checksum]
            //   [x30,x29] [x28,x27] [x26,x25] [x24,x23] [x22,x21] [x20,x19]
            //   [d15,d14] [d13,d12] [d11,d10] [d9,d8]
            //   [nzcv] [checksum]
            //
            // x0 points to the start of this buffer (top, since we push down).

            // ── Pop checksum ──
            c.AddLine("ldp xzr, x10, [x0], #16");
            EmitMovImm64(c, "x11", STACK_CHUCK_SUM);
            c.AddLine("cmp x10, x11");
            A64Assert(c);

            // ── Pop LR ──
            c.AddLine("ldp xzr, x30, [x0], #16");

            // ── Pop checksum ──
            c.AddLine("ldp xzr, x10, [x0], #16");
            c.AddLine("cmp x10, x11");
            A64Assert(c);

            // ── Pop SP → restore later ──
            c.AddLine("ldp xzr, x12, [x0], #16");  // x12 = original SP

            // ── Pop checksum ──
            c.AddLine("ldp xzr, x10, [x0], #16");
            c.AddLine("cmp x10, x11");
            A64Assert(c);

            // ── Pop callee-saved GPRs ──
            c.AddLine("ldp x19, x20, [x0], #16");
            c.AddLine("ldp x21, x22, [x0], #16");
            c.AddLine("ldp x23, x24, [x0], #16");
            c.AddLine("ldp x25, x26, [x0], #16");
            c.AddLine("ldp x27, x28, [x0], #16");
            c.AddLine("ldp x29, x30, [x0], #16");

            // ── Pop d8–d15 ──
            c.AddLine("ldp d8, d9, [x0], #16");
            c.AddLine("ldp d10, d11, [x0], #16");
            c.AddLine("ldp d12, d13, [x0], #16");
            c.AddLine("ldp d14, d15, [x0], #16");

            // ── Pop NZCV ──
            c.AddLine("ldp xzr, x10, [x0], #16");
            c.AddLine("msr nzcv, x10");

            // ── Pop checksum ──
            c.AddLine("ldp xzr, x10, [x0], #16");
            EmitMovImm64(c, "x13", STACK_CHUCK_SUM);
            c.AddLine("cmp x10, x13");
            A64Assert(c);

            // ── Restore SP ──
            c.AddLine("mov sp, x12");

            // ── Load return value into x0 ──
            c.AddLine("mov x0, x13");

            // ── Jump via LR ──
            c.AddLine("ret");
        }

        // ── ARM64-specific assert helper (replaces x86 je/int3 with b.eq/brk) ─

        private static void A64Assert(AsmAssembler c)
        {
            var suc = c.CreateLabel();
            c.AddLine($"b.eq {suc}");
            c.AddLine("brk #0");
            c.Label(ref suc);
        }

        // ── Default empty method (ARM64 ret) ────────────────────────────

        protected override void Generate_asm_empty_method(AsmAssembler c)
        {
            c.AddLine("ret");
        }
    }
}
