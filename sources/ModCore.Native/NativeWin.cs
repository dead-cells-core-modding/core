extern alias iced;

using iced::Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using static Windows.Win32.PInvoke;

using static iced::Iced.Intel.AssemblerRegisters;
using Decoder = iced::Iced.Intel.Decoder;
using Hashlink;
using Windows.Win32.Foundation;
using System.Diagnostics;

#pragma warning disable CA1416

namespace ModCore.Native
{
    [SupportedOSPlatform("windows")]
    internal unsafe partial class NativeWin : Native
    {
        [LibraryImport("Kernel32")]
        private static partial int GetThreadContext( nint handle, void* ctx );
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
         * Get the return address storage location to facilitate return address hijacking
         * 
         */
        protected override void Generate_asm_hl2cs_store_return_ptr( Assembler c )
        {
            c.push(rcx);


            c.pop(rcx);

            c.mov(rax, (long)&Data->dotnet_runtime_pinvoke_wrapper);
            c.jmp(__qword_ptr[rax]);
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
        protected override void Generate_asm_hook_break_on_trap_Entry( Assembler c )
        {
            var fallback = c.CreateLabel();

            c.push(rcx);
            c.push(rdx);
            c.push(r8);
            c.push(r9);

            c.sub(rsp, 40);

            c.mov(rax, (long)&Data->trap_filter);
            c.mov(r11, __[rax]);
            c.call(r11);

            c.add(rsp, 40);

            c.cmp(rax, 0xff);
            c.jl(fallback);

            // Restore
            // This operation must be performed in an unmanaged environment

            c.mov(rsp, __[rax + 16]);

            c.pop(r10); //Checksum
            c.cmp(r10, STACK_CHUCK_SUM);
            c.jnz(c.F);

            c.pop(r15);
            c.pop(r14);
            c.pop(r13);
            c.pop(r12);
            c.pop(rsi);
            c.pop(rdi);
            c.pop(rbp);
            c.pop(rbx);
            c.pop(rax);

            c.mov(rsp, rax);
            //c.ret();

            c.mov(rax, (long)&Data->return_from_managed);
            c.mov(r11, __[rax]);

            c.jmp(r11);

            c.AnonymousLabel();
            c.int3();


            // Fallback

            c.Label(ref fallback);
            c.pop(r9);
            c.pop(r8);
            c.pop(rdx);
            c.pop(rcx);

            c.mov(rax, (long)&Data->orig_break_on_trap);
            c.mov(r11, __[rax]);
            c.jmp(r11);
        }

        /**
         * 
         * if(hModule == phLibhl) {
         *  return new_GetProcAddress(hModule, hName, unknown);
         * } else {
         *  return orig(hModule, hName, unknown);
         * }
         * 
         */
        protected override void Generate_asm_hook_GetProcAddress_Entry( Assembler c )
        {
            var fallback = c.CreateLabel();

            c.mov(rax, (long)&Data->phLibhl);
            c.cmp(rcx, __[rax]);
            c.jnz(fallback);

            c.mov(rax, (long)&Data->new_GetProcAddress);
            c.mov(r11, __[rax]);
            c.jmp(r11);

            c.Label(ref fallback);

            c.mov(rax, (long)&Data->orig_GetProcAddress);
            c.mov(r11, __[rax]);
            c.jmp(r11);
        }

        protected virtual void Generate_asm_cs_hl_store_context( Assembler c )
        {
            c.lea(rax, __[rsp + 8]); //Original Stack

            c.mov(r11, __[rsp]); //Data Table Pointer
            c.mov(rsp, __[r11]); //Register Store

            c.push(rax); //RSP
            c.push(rbx);
            c.push(rbp);
            c.push(rdi);
            c.push(rsi);
            c.push(r12);
            c.push(r13);
            c.push(r14);
            c.push(r15);

            //Checksum
            c.mov(r10, STACK_CHUCK_SUM);
            c.push(r10);

            c.mov(__[r11 + 16], rsp); //Save rsp (Register store)

            c.mov(rsp, rax);
            c.mov(rax, __[r11 + 8]); //Target 

            c.jmp(rax);
        }

        public override unsafe void FixThreadCurrentStackFrame( HL_thread_info* t )
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
                | Windows.Win32.System.Threading.THREAD_ACCESS_RIGHTS.THREAD_SUSPEND_RESUME, false, (uint) t->thread_id);

            
            SuspendThread(th);

            byte* buffer = stackalloc byte[2048];
            *((int*)(buffer + 48)) = (0x00100000 | 0x00000001);
            var err = GetThreadContext(th.DangerousGetHandle(), buffer);
            Debug.Assert(err != 0);
            var rsp = *((nint*)(buffer + 152));
            Debug.Assert(rsp != 0);

            t->stack_cur = (void*) rsp;

            ResumeThread(th);
        }
    }
}
