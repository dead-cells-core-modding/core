
extern alias iced;

using Hashlink;

using iced::Iced.Intel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static iced::Iced.Intel.AssemblerRegisters;
using Decoder = iced::Iced.Intel.Decoder;

namespace ModCore.Native
{
    internal unsafe static class NativeAsm
    {
        private static readonly nint nativeCodePage;

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeAsmData
        {
            // Hook GetProcAddress

            public nint orig_GetProcAddress;
            public nint new_GetProcAddress;
            public nint phLibhl;

            // Hook break_on_trap

            public nint orig_break_on_trap;
            public nint trap_filter;
        }

        public static NativeAsmData* Data
        {
            get;
        } = (NativeAsmData*)NativeMemory.AlignedAlloc(
            (nuint)sizeof(NativeAsmData), 8);
        
        static NativeAsm()
        {
            nativeCodePage = (nint)HashlinkNative.hl_alloc_executable_memory(8192);
            using var stream = new UnmanagedMemoryStream((byte*)nativeCodePage, 8192, 8192, FileAccess.ReadWrite);
            foreach (var v in typeof(NativeAsm).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                var generator = typeof(NativeAsm).GetMethod("Generate_" + v.Name, BindingFlags.Static | BindingFlags.NonPublic);

                Debug.Assert(generator != null);

                var start = stream.PositionPointer;

                var assembler = new Assembler(64);
                generator.Invoke(null, [assembler]);

                assembler.int3();

                assembler.Assemble(new StreamCodeWriter(stream), 
                    (ulong)start);

                v.SetValue(null, (nint)start);
            }
        }

        public static nint hook_break_on_trap_Entry;

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
        private static void Generate_hook_break_on_trap_Entry( Assembler c )
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                c.ret();

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
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static nint hook_GetProcAddress_Entry;

        /**
         * 
         * if(hModule == phLibhl) {
         *  return new_GetProcAddress(hModule, hName, unknown);
         * } else {
         *  return orig(hModule, hName, unknown);
         * }
         * 
         */
        private static void Generate_hook_GetProcAddress_Entry( Assembler c )
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
        public static nint empty_method;
        private static void Generate_empty_method( Assembler c )
        {
            c.ret();
        }
        public static nint cs_hl_store_context;
        private static void Generate_cs_hl_store_context(Assembler c )
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

                c.mov(__[r11 + 16], rsp); //Save rsp (Register store)
                c.mov(rsp, rax);
                c.mov(rax, __[r11 + 8]); //Target 
              
                c.jmp(rax);

                return;
            }
            throw new NotSupportedException();
        }

        public static nint cs_hl_return_from_exception;
        private static void Generate_cs_hl_return_from_exception( Assembler c )
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                c.mov(rsp, __[rcx + 16]);

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
                c.ret();

                return;
            }
            throw new NotSupportedException();
        }
    }
}
