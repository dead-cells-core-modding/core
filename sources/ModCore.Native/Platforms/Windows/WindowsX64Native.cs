using Hashlink;
using ModCore.Storage;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Diagnostics.Debug;
using static Windows.Win32.PInvoke;
using R = ModCore.Native.AsmAssembler.R;

#pragma warning disable CA1416

namespace ModCore.Native
{
    [SupportedOSPlatform("windows")]
    internal unsafe partial class WindowsX64Native : Native
    {
        [LibraryImport("modcorenative", EntryPoint = "init_veh")]
        private static partial void InitVEH( nint createDumpCommand );

        public override void MakePageWritable( nint ptr, out int old )
        {
            var pageStart = ptr & ~(Environment.SystemPageSize - 1);
            VirtualProtect((void*)pageStart, (nuint)Environment.SystemPageSize,
                Windows.Win32.System.Memory.PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE, out var oldFlags);
            old = (int)oldFlags;
        }

        public override void RestorePageProtect( nint ptr, int val )
        {
            var pageStart = ptr & ~(Environment.SystemPageSize - 1);
            VirtualProtect((void*)pageStart, (nuint)Environment.SystemPageSize,
                (Windows.Win32.System.Memory.PAGE_PROTECTION_FLAGS)val, out _);
        }

        public override nint GetCurrentThreadStackBase()
        {
            GetCurrentThreadStackLimits(out _, out var highLimit);
            return (nint)highLimit;
        }

        [UnmanagedCallersOnly]
        protected static int Hook_throw_handler( int code )
        {
            return 0;
        }

        public override void InitializeNative()
        {
            base.InitializeNative();

            {
                var dmpPath = FolderInfo.Cache.GetFilePath("crash.dmp");
                if (File.Exists(dmpPath))
                {
                    File.Delete(dmpPath);
                }

                //Console.Error.WriteLine("[DCCMDBG-CRASH]" + dmpPath);

                //var rtRoot = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
                //var createDumpPath = Path.Combine(rtRoot, "createdump.exe");
                //var createDumpCmd = $"\"{createDumpPath}\" -f \"{dmpPath}\" -n";

                //InitVEH(Marshal.StringToHGlobalUni(createDumpCmd));
            }
        }

        protected override void InitializeAsm()
        {
            base.InitializeAsm();
        }

        public override string[] GetDisplayDevices()
        {
            List<string> devices = [];
            DISPLAY_DEVICEW ddevice = new()
            {
                cb = (uint)sizeof(DISPLAY_DEVICEW)
            };

            uint index = 0;
            while (EnumDisplayDevices(null, index++, ref ddevice, 0))
            {
                devices.Add(new string(ddevice.DeviceString.Value).Trim());
            }
            return [.. devices];
        }

        protected override void InitializeNativeHooks()
        {
            base.InitializeNativeHooks();

            try
            {
                CreateNativeHookForHL("global_handler", nameof(Hook_throw_handler), out _);
            }
            catch (Exception) { }
        }

        public override ReadOnlySpan<byte> GetHlbootDataFromExe( string exePath )
        {
            var hExe = LoadLibraryEx(exePath,
                 Windows.Win32.System.LibraryLoader.LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_AS_DATAFILE |
                 Windows.Win32.System.LibraryLoader.LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_AS_IMAGE_RESOURCE);

            if (hExe.IsInvalid)
            {
                return default;
            }

            var res = FindResource(hExe, "hlboot.dat", "#10");
            if (res.IsNull)
            {
                hExe.Dispose();
                return default;
            }

            var size = SizeofResource(hExe, res);
            var hres = LoadResource(hExe, res);
            if (hres.IsInvalid)
            {
                hExe.Dispose();
                return default;
            }
            var ptr = LockResource(hres);

            hExe.SetHandleAsInvalid();
            hres.SetHandleAsInvalid();
            return new(ptr, (int)size);
        }

        /***
         * This operation must be performed in an unmanaged environment
         */
        protected override void Generate_asm_hl2cs_throw_exception( AsmAssembler c )
        {
            AsmGetTlsDataPtrRax(c, ref tls_template->prev_hl_error_ptr);

            // mov rcx, [rax]   →   movq (%rax), %rcx
            c.mov_mr(R.rcx, R.rax);

            // sub rsp, 56
            c.sub(R.rsp, 56);

            AsmGetTlsDataPtrRax(c, ref tls_template->hl_throw_ptr);

            // jmp qword ptr [rax]   →   jmpq *(%rax)
            c.jmp_m(R.rax);
        }

        /***
         * This operation must be performed in an unmanaged environment
         * Get the return address storage location to facilitate return address hijacking
         */
        protected override void Generate_asm_hl2cs_store_return_ptr( AsmAssembler c )
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

        /**
         *
         * void* result = trap_filter(t, ctx, v);
         * if(result < 0xff) {
         *  return orig(t, ctx, v);
         * }
         *
         * RestoreStack();
         *
         */
        protected override void Generate_asm_hook_break_on_trap_Entry( AsmAssembler c )
        {
            var fallback = c.CreateLabel();

            // ── Save argument registers (MS x64 ABI) ──────────────────
            c.push(R.rcx);
            c.push(R.rdx);
            c.push(R.r8);
            c.push(R.r9);

            // Align stack: 4 pushes = -32, need 16-byte alignment before call.
            // Entry rsp == 8 (mod 16).  4 pushes → rsp == 8 (mod 16).
            // sub 40 → rsp == 0 (mod 16) for the call.
            c.sub(R.rsp, 40);

            // Call orig_break_on_trap
            c.mov_imm(R.rax, (long)&Data->orig_break_on_trap);
            c.mov_mr(R.r11, R.rax);
            c.call_r(R.r11);

            c.add(R.rsp, 40);

            // Restore argument registers in reverse order
            c.pop(R.r9);
            c.pop(R.r8);
            c.pop(R.rdx);
            c.pop(R.rcx);

            // Align + call trap_filter(t, ctx, v)
            c.sub(R.rsp, 40);

            c.mov_imm(R.rax, (long)&Data->trap_filter);
            c.mov_mr(R.r11, R.rax);
            c.call_r(R.r11);

            c.add(R.rsp, 40);

            // ── Check result ──────────────────────────────────────────
            // cmp rax, 0xff
            c.cmp_ri(R.rax, 0xff);
            c.jl(fallback);

            // ── Restore execution context ─────────────────────────────
            // This section restores from the format written by
            // Generate_asm_cs_hl_store_context.

            // mov rsp, [rax + 16]   →   movq 16(%rax), %rsp
            c.mov_mr(R.rsp, R.rax, 16);

            // pop r10  (Checksum)
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

            // pop r10  (Checksum)
            c.pop(R.r10);
            c.cmp_ri(R.r10, STACK_CHUCK_SUM);
            Assert(c);

            c.pop(R.rax);

            // pop r10  (Checksum)
            c.pop(R.r10);
            c.cmp_ri(R.r10, STACK_CHUCK_SUM);
            Assert(c);

            // pop r11  (Saved return IP)
            c.pop(R.r11);

            // mov rsp, rax   →   movq %rax, %rsp
            c.mov_rr(R.rsp, R.rax);

            // mov rax, &Data->return_from_managed
            c.mov_imm(R.rax, (long)&Data->return_from_managed);

            // mov [rsp], r11   →   movq %r11, (%rsp)   (Fix return ptr)
            // It's dangerous but effective
            c.mov_rm(R.r11, R.rsp);

            // jmp qword ptr [rax]   →   jmpq *(%rax)
            c.jmp_m(R.rax);

            c.AnonymousLabel();
            c.int3();

            // ── Fallback ──────────────────────────────────────────────
            c.Label(ref fallback);

            // mov rax, 0; ret
            c.mov_imm(R.rax, 0);
            c.ret();

            // Unreachable tail — jump through orig_break_on_trap
            c.mov_imm(R.rax, (long)&Data->orig_break_on_trap);
            c.jmp_m(R.rax);
        }

        protected override void Generate_asm_cs_hl_store_context( AsmAssembler c )
        {
            // pop r11   (Data table pointer pushed by caller)
            c.pop(R.r11);

            // mov r10, [rsp]   →   movq (%rsp), %r10   (Return IP)
            c.mov_mr(R.r10, R.rsp);

            // mov rax, rsp   →   movq %rsp, %rax   (Original stack)
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

        public override void FixThreadCurrentStackFrame( HL_thread_info* t )
        {
            if (!Environment.Is64BitProcess)
            {
                throw new PlatformNotSupportedException();
            }
            if (t->thread_id == GetCurrentThreadId())
            {
                t->stack_cur = &t;
                return;
            }
            using var th = OpenThread_SafeHandle(Windows.Win32.System.Threading.THREAD_ACCESS_RIGHTS.THREAD_GET_CONTEXT
                | Windows.Win32.System.Threading.THREAD_ACCESS_RIGHTS.THREAD_SUSPEND_RESUME, false, (uint)t->thread_id);

            SuspendThread(th);

            CONTEXT* context = stackalloc CONTEXT[1];

            context->ContextFlags = CONTEXT_FLAGS.CONTEXT_CONTROL_AMD64;

            var err = GetThreadContext(th, ref context[0]);

            Debug.Assert(err != 0);

            var rsp = context->Rsp;

            Debug.Assert(rsp != 0);

            t->stack_cur = (void*)rsp;

            ResumeThread(th);
        }

        public override void SetTlsValue( int index, nint val )
        {
            TlsSetValue((uint)index, (void*)val);
        }

        public override nint GetTlsValue( int index )
        {
            return (nint)TlsGetValue((uint)index);
        }

        protected override void AsmGetTlsDataPtrRax<T>( AsmAssembler c, ref T offset )
        {
            var ofs = (nint)Unsafe.AsPointer(ref offset) - (nint)tls_template;
            var tls_id = Data->tls_slot_index;

            // push rcx   (preserve caller's rcx across TLS access)
            c.push(R.rcx);

            if (tls_id < 0x40)
            {
                // mov rcx, gs:[5248 + tls_id * 8]   →   movq %gs:off, %rcx
                c.mov_gs(R.rcx, 5248 + tls_id * 8);
            }
            else
            {
                // mov rax, gs:[0x1780]   →   movq %gs:0x1780, %rax
                c.mov_gs(R.rax, 0x1780);
                // mov rcx, [rax + 8 * (tls_id - 64)]   →   movq off(%rax), %rcx
                c.mov_mr(R.rcx, R.rax, 8 * (tls_id - 64));
            }

            // lea rax, [rcx + ofs]   →   leaq ofs(%rcx), %rax
            c.lea(R.rax, R.rcx, (int)ofs);

            // pop rcx   (restore caller's rcx)
            c.pop(R.rcx);
        }

        public override int AllocTls()
        {
            return (int)TlsAlloc();
        }

        public override bool IsBadPtr( nint ptr )
        {
            return IsBadReadPtr((void*)ptr, 8) && IsBadWritePtr((void*)ptr, 8) && IsBadCodePtr((FARPROC)ptr);
        }

        /// <summary>
        /// Custom longjmp for HLC-compiled code on Windows x64.
        ///
        /// Translates the HashLink JIT longjmp logic:
        ///   buf  → rcx (first MS x64 arg)
        ///   ret  → rdx (second MS x64 arg)
        ///
        /// Restore all callee-saved registers, control word, XMM6–XMM15,
        /// and jump to the saved return address.
        /// </summary>
        protected override void Generate_asm_custom_longjump( AsmAssembler c )
        {
            // rax = ret value (rdx → rax)
            // mov rax, rdx   →   movq %rdx, %rax
            c.mov_rr(R.rax, R.rdx);

            // Restore callee-saved GPRs from jmp_buf
            // mov rdx, [rcx + 0x00]
            c.mov_mr(R.rdx, R.rcx, 0x00);
            // mov rbx, [rcx + 0x08]
            c.mov_mr(R.rbx, R.rcx, 0x08);
            // mov rsp, [rcx + 0x10]
            c.mov_mr(R.rsp, R.rcx, 0x10);
            // mov rbp, [rcx + 0x18]
            c.mov_mr(R.rbp, R.rcx, 0x18);
            // mov rsi, [rcx + 0x20]
            c.mov_mr(R.rsi, R.rcx, 0x20);
            // mov rdi, [rcx + 0x28]
            c.mov_mr(R.rdi, R.rcx, 0x28);
            // mov r12, [rcx + 0x30]
            c.mov_mr(R.r12, R.rcx, 0x30);
            // mov r13, [rcx + 0x38]
            c.mov_mr(R.r13, R.rcx, 0x38);
            // mov r14, [rcx + 0x40]
            c.mov_mr(R.r14, R.rcx, 0x40);
            // mov r15, [rcx + 0x48]
            c.mov_mr(R.r15, R.rcx, 0x48);

            // ldmxcsr [rcx + 0x58]
            c.ldmxcsr(R.rcx, 0x58);
            // fldcw [rcx + 0x5C]
            c.fldcw(R.rcx, 0x5C);

            // Restore XMM6–XMM15 from jmp_buf (offset 0x60 + i*16)
            var xmms = new[] {
                R.xmm6,  R.xmm7,  R.xmm8,  R.xmm9,  R.xmm10,
                R.xmm11, R.xmm12, R.xmm13, R.xmm14, R.xmm15
            };
            for (int i = 0; i < xmms.Length; i++)
            {
                // movsd xmmN, [rcx + 0x60 + i*16]   →   movsd off(%rcx), %xmmN
                c.movsd_rm(xmms[i], R.rcx, i * 16 + 0x60);
            }

            // push qword ptr [rcx + 0x50]   →   pushq 0x50(%rcx)
            c.push_m(R.rcx, 0x50);

            // ret
            c.ret();
        }
    }
}
