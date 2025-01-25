using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Hashlink.Trace;
using ModCore;
using MonoMod.Core.Platforms;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Brigde
{
    internal unsafe static class MethodWrapperFactory
    {
        private readonly static int PAGE_ALLOC_SIZE = 8192;
        private static readonly byte[] call_code_x64 = [
            0x48, 0xB8, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, //mov rax, 0xffffffffffffffff
            0xFF, 0xD0 //call rax
            ];
        private static readonly byte[] call_code_x86 = [
            0xB8, 0XFF, 0xFF, 0xFF, 0xFF, //mov eax, 0xffffffff
            0xFF, 0xD0 //call eax
            ];

        private static readonly void* csentry_no_orig_ptr = (delegate* unmanaged[Cdecl]<void>)&CSEntry_NoOriginal;
        private static readonly void* csentry_ptr = (delegate* unmanaged[Cdecl]<NativeInfoTable*, void*, long*, void**, void>)&CSEntry;
        private static readonly Queue<nint> freeEntries = [];

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static EntryItem* CreateWrapper(MethodWrapper wrapper,
            IEnumerable<HL_type.TypeKind> argTypes, HL_type.TypeKind retType)
        {
            if (freeEntries.Count == 0)
            {
                //Allocate New Page
                void* page = hl_alloc_executable_memory(PAGE_ALLOC_SIZE);
                for(int i = 0; i < PAGE_ALLOC_SIZE; i+= sizeof(EntryItem))
                {
                    freeEntries.Enqueue((nint)page + i);
                }
            }
            EntryItem* entry = (EntryItem*)freeEntries.Dequeue();
            
            InitEntryItem(entry);
            var table = &entry->table;
            table->wrapperHandle = (nint)GCHandle.Alloc(wrapper, GCHandleType.Normal);

            //

            table->callback = (nint)csentry_ptr;

            table->tret = retType;

            if (retType == HL_type.TypeKind.HF32 ||
                retType == HL_type.TypeKind.HF64)
            {
                table->retType = 1;
            }
            else if (!Environment.Is64BitProcess && retType == HL_type.TypeKind.HI64)
            {
                table->retType = 2;
            }
            else
            {
                table->retType = 0;
            }

            //

            foreach (var at in argTypes)
            {
                table->targs[table->argsCount] = (int)at;
                table->argsCount++;

                if(at == HL_type.TypeKind.HF32 ||
                    at == HL_type.TypeKind.HF64)
                {
                    table->argFloatMarks |= 1;
                    if(at == HL_type.TypeKind.HF64)
                    {
                        table->argSizeBitMarks |= 1;
                    }
                }
                else
                {
                    if(at.IsPointer() && Environment.Is64BitProcess)
                    {
                        table->argSizeBitMarks |= 1;
                    }
                    else if(at == HL_type.TypeKind.HI64)
                    {
                        table->argSizeBitMarks |= 1;
                    }
                }

                table->argSizeBitMarks <<= 1;
                table->argFloatMarks <<= 1;
            }

            //

            table->argSizeBitMarks = Utils.ReverseBits(table->argSizeBitMarks) >> (32 - table->argsCount);
            table->argFloatMarks = Utils.ReverseBits(table->argFloatMarks) >> (32 - table->argsCount);


            return entry;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void FreeWrapper(MethodWrapper wrapper)
        {
            if(wrapper.EntryHandle == null)
            {
                return;
            }
            var table = &wrapper.EntryHandle->table;
            GCHandle.FromIntPtr(table->wrapperHandle).Free();

            table->wrapperHandle = 0;
            freeEntries.Enqueue((nint)wrapper.EntryHandle);
        }

        private static void* InitEntryItem(EntryItem* item)
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
        private static void CSEntry(NativeInfoTable* table, void* retVal, long* args, void** err)
        {
            if(table->wrapperHandle == 0 ||
                table->enabled == 0)
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
                *err = (void*)new HashlinkNETExceptionObj(ex).HashlinkPointer;
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
            public int retType; /* 2: return int64 in x86, 1: return float/double, 0: return ptr/int32/int16/byte/others */
            public nint origFuncPtr;
            public int enabled; //Invalid, reserved
            public int argsCount;
            public uint argSizeBitMarks; /* bit marks : 1 means the parameter is 8 bytes long, otherwise it is 4 bytes long */
            public uint argFloatMarks; /* bit marks : 1 means the parameter is float/double, otherwise it is int/ptr */
            public nint callback;

            //The following fields are not visible to the native layer

            public fixed int targs[32];
            public HL_type.TypeKind tret;

            public void* entryPtr;
            public nint wrapperHandle;
        }
    }
}
