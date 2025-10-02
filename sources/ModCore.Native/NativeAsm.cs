
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static iced::Iced.Intel.AssemblerRegisters;
using Decoder = iced::Iced.Intel.Decoder;

namespace ModCore.Native
{
    internal unsafe partial class Native
    {
        public const int STACK_CHUCK_SUM = unchecked((int)0xcececece);
        private nint nativeCodePage;

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
            public nint return_from_managed;

            // hl2cs helper

            public nint hl_throw;
            public nint dotnet_runtime_pinvoke_wrapper;
            public nint capture_current_frame;
        }

        public NativeAsmData* Data
        {
            get;
        } = (NativeAsmData*)NativeMemory.AlignedAlloc(
            (nuint)sizeof(NativeAsmData), 16);

        protected virtual void InitializeAsm()
        {
            nativeCodePage = (nint)HashlinkNative.hl_alloc_executable_memory(8192);

            *Data = new();
            var st = GetType();

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

                var start = stream.PositionPointer;

                var assembler = new Assembler(64);
                generator.Invoke(this, [assembler]);

                assembler.int3();

                assembler.Assemble(new StreamCodeWriter(stream), 
                    (ulong)start);

                v.SetValue(this, (nint)start);
            }
        }

       
        public nint asm_empty_method;
        protected virtual  void Generate_asm_empty_method( Assembler c )
        {
            c.ret();
        }
        public nint asm_cs_hl_store_context;

        public nint asm_hl2cs_store_return_ptr;

        public nint asm_hook_break_on_trap_Entry;

        public nint asm_hook_GetProcAddress_Entry;


        protected abstract void Generate_asm_hl2cs_store_return_ptr( Assembler c );
        protected abstract void Generate_asm_hook_break_on_trap_Entry( Assembler c );
        protected abstract void Generate_asm_hook_GetProcAddress_Entry( Assembler c );

    }
}
