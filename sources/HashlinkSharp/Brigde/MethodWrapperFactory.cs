using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using Hashlink.Trace;
using ModCore;
using Serilog;
using Serilog.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hashlink.Brigde
{
    internal static unsafe class MethodWrapperFactory
    {
        private static readonly int PAGE_ALLOC_SIZE = 8192;
        private static readonly byte[] call_code_x64 = [
            0x48, 0xB8, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, //mov rax, 0xffffffffffffffff
            0xFF, 0xD0 //call rax
            ];
        private static readonly byte[] call_code_x86 = [
            0xB8, 0XFF, 0xFF, 0xFF, 0xFF, //mov eax, 0xffffffff
            0xFF, 0xD0 //call eax
            ];

        private static readonly void* csentry_no_orig_ptr = (delegate* unmanaged[Cdecl]< void >)&CSEntry_NoOriginal;
        private static readonly void* csentry_ptr = (delegate* unmanaged[Cdecl]< NativeInfoTable*, void*, long*, void**, void >)&CSEntry;
        private static readonly Queue<nint> freeEntries = [];

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static EntryItem* CreateWrapper( MethodWrapper wrapper,
            IEnumerable<HashlinkType> argTypes, HashlinkType retType )
        {
            if (freeEntries.Count == 0)
            {
                //Allocate New Page
                var page = hl_alloc_executable_memory(PAGE_ALLOC_SIZE);
                for (var i = 0; i < PAGE_ALLOC_SIZE; i += sizeof(EntryItem))
                {
                    freeEntries.Enqueue((nint)page + i);
                }
            }
            var entry = (EntryItem*)freeEntries.Dequeue();

            InitEntryItem(entry);
            var table = &entry->table;
            table->wrapperHandle = (nint)GCHandle.Alloc(wrapper, GCHandleType.Normal);

            //

            table->callback = (nint)csentry_ptr;

            table->retType = retType.TypeKind is TypeKind.HF32 or TypeKind.HF64
                ? 1
                : 0;

            //

            foreach (var at in argTypes)
            {
                table->argsCount++;

                if (at.TypeKind is TypeKind.HF32 or
                    TypeKind.HF64)
                {
                    table->hasFloatArg = 1;
                    table->argFloatMarks |= 1;
                }
                table->argFloatMarks <<= 1;
            }

            table->argFloatMarks >>= 1;

            //

            table->argFloatMarks = Utils.ReverseBits(table->argFloatMarks) >> (32 - table->argsCount);


            return entry;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void FreeWrapper( MethodWrapper wrapper )
        {
            if (wrapper.EntryHandle == null)
            {
                return;
            }
            var table = &wrapper.EntryHandle->table;
            GCHandle.FromIntPtr(table->wrapperHandle).Free();

            table->wrapperHandle = 0;
            freeEntries.Enqueue((nint)wrapper.EntryHandle);
        }

        private static void* InitEntryItem( EntryItem* item )
        {
            item->table = new();
            if (Environment.Is64BitProcess)
            {
                fixed (byte* code = call_code_x64)
                {
                    Buffer.MemoryCopy(code, item->call_code_x64, call_code_x64.LongLength, call_code_x64.LongLength);
                }
                *(void**)(item->call_code_x64 + 2) = Native.get_asm_call_bridge_hl_to_cs();
                return item->table.entryPtr = item->call_code_x64;
            }
            else
            {
                fixed (byte* code = call_code_x86)
                {
                    Buffer.MemoryCopy(code, item->call_code_x86, call_code_x86.LongLength, call_code_x86.LongLength);
                }
                *(void**)(item->call_code_x86 + 1) = Native.get_asm_call_bridge_hl_to_cs();
                return item->table.entryPtr = item->call_code_x86;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        [CallFromHLOnly]
        [StackTraceHidden]
        private static void CSEntry_NoOriginal()
        {
            throw new InvalidOperationException("Trying to call an empty CSharp Method Wrapper");
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        [CallFromHLOnly]
        [StackTraceHidden]
        private static void CSEntry( NativeInfoTable* table, void* retVal, long* args, void** err )
        {
            if (table->wrapperHandle == 0)
            {
                throw new InvalidOperationException();
            }
            var gch = GCHandle.FromIntPtr(table->wrapperHandle);
            var wrapper = (MethodWrapper?)gch.Target ?? throw new InvalidOperationException();
            try
            {
                wrapper.Entry(table, retVal, args);
            }
            catch (HashlinkError ex)
            {
                *err = (void*)ex.Error;
            }
            catch (Exception ex)
            {
                //Direct fatal until I fix it
                Log.Logger.Fatal(ex, "MethodWrapper fatal.");
                Environment.FailFast(ex.ToString());
                //*err = (void*)new HashlinkNETExceptionObj(ex).HashlinkPointer;
            }
        }


        [StructLayout(LayoutKind.Explicit)]
        public struct EntryItem
        {
            [FieldOffset(0)]
            public fixed byte call_code_x64[1];
            [FieldOffset(5)]
            public fixed byte call_code_x86[1];
            [FieldOffset(12)]
            public NativeInfoTable table;

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeInfoTable
        {
            //The following fields should be consistent with the hl2c_table in modcorenative.h
            public int retType; /* 1: return float/double, 0: return ptr/int32/int16/byte/others */
            public int hasFloatArg; /* 1: Has float/double return value 0: hasn't */
            public uint argFloatMarks; /* bit marks : 1 means the parameter is float/double, otherwise it is int/ptr */
            public nint origFuncPtr;
            public int argsCount;
            public nint callback;

            //The following fields are not visible to the native layer

            public void* entryPtr;
            public nint wrapperHandle;
        }
    }
}
