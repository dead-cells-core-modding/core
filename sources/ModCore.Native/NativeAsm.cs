
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

                assembler.Assemble(new StreamCodeWriter(stream), 
                    (ulong)start);

                v.SetValue(null, (nint)start);
            }
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
