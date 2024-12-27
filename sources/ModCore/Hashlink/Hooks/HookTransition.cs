using Hashlink;
using ModCore.Modules;
using ModCore.Track;
using MonoMod.Core.Platforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hashlink.Hooks
{
    internal static unsafe class HookTransition
    {
        private readonly static int ENTRYS_COUNT_PER_PAGE = 4096 / sizeof(EntryItem);
        private static readonly byte[] call_code_x64 = [
            0x48, 0xB8, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, //mov rax, 0xffffffffffffffff
            0xFF, 0xD0 //call rax
            ];
        private static readonly byte[] call_code_x86 = [
            0xB8, 0XFF, 0xFF, 0xFF, 0xFF, //mov eax, 0xffffffff
            0xFF, 0xD0 //call eax
            ];
        private static readonly void* csentry_ptr = (delegate* unmanaged[Cdecl]<HookTable*, void*, long*, void>)&CSEntry;

        private static readonly List<IAllocatedMemory> csentry_pages = [];
        private static EntryItem* cur_entry_page;
        private static int cur_entry_page_offset;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static HashlinkHookInst CreateHook(HL_function* target)
        {
            if (cur_entry_page == null || cur_entry_page_offset >= ENTRYS_COUNT_PER_PAGE)
            {
                cur_entry_page_offset = 0;
                if (!PlatformTriple.Current.System.MemoryAllocator.TryAllocate(
                    new(
                        sizeof(EntryItem) * ENTRYS_COUNT_PER_PAGE
                        )
                    {
                        Executable = true
                    }, out var allocated))
                {
                    throw new NullReferenceException();
                }
                csentry_pages.Add(allocated);
                cur_entry_page = (EntryItem*)allocated.BaseAddress;
            }

            var entry = cur_entry_page + cur_entry_page_offset;
            var table = &entry->table;
            cur_entry_page_offset++;
            InitEntryItem(entry);

            var entryPtr = Environment.Is64BitProcess ? (nint)entry->call_code_x64 : (nint)entry->call_code_x86;
            var hook = NativeHook.Instance.CreateHook(
                (nint)HashlinkUtils.GetFunctionNativePtr(target),
                entryPtr
                );
            var inst = new HashlinkHookInst(table, hook);
            entry->table.chainHandle = (nint)GCHandle.Alloc(inst);

            {
                var f = target->type->data.func;
                table->origFuncPtr = (void*)hook.Original;
                table->argsCount = f->nargs;
                table->func = target;
                table->funcIndex = target->findex;
                table->callback = (nint)csentry_ptr;
                if (f->ret->kind == HL_type.TypeKind.HF32 ||
                    f->ret->kind == HL_type.TypeKind.HF64)
                {
                    table->retType = 1;
                }
                else if (!Environment.Is64BitProcess && f->ret->kind == HL_type.TypeKind.HI64)
                {
                    table->retType = 2;
                }
                else
                {
                    table->retType = 0;
                }

                table->argBitMarks = 0;
                for (int i = table->argsCount - 1; i >= 0; i--)
                {
                    table->argBitMarks <<= 1;
                    var at = f->args[i];
                    if (at->kind.IsPointer())
                    {
                        if (Environment.Is64BitProcess)
                        {
                            table->argBitMarks |= 1;
                        }
                        continue;
                    }
                    if (at->kind == HL_type.TypeKind.HI64 ||
                        at->kind == HL_type.TypeKind.HF64)
                    {
                        table->argBitMarks |= 1;
                    }
                }

            }

            return inst;
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
                return item->call_code_x64;
            }
            else
            {
                fixed (byte* code = call_code_x86)
                {
                    Buffer.MemoryCopy(code, item->call_code_x86, call_code_x86.LongLength, call_code_x86.LongLength);
                }
                *(void**)(item->call_code_x86 + 1) = Native.get_asm_call_bridge_hl_to_cs();
                return item->call_code_x86;
            }
        }


        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        [CallFromHLOnly]
        [StackTraceHidden]
        private static void CSEntry(HookTable* table, void* retVal, long* args)
        {
            var chain = (HashlinkHookInst?)GCHandle.FromIntPtr(table->chainHandle).Target;
            if (chain == null || chain.chain == null)
            {
                //This shouldn't exist.
                throw new InvalidProgramException();
            }

            var delegates = chain.chain.GetInvocationList();
            var orig = new HashlinkFunc(delegates, 1, table->func, table->origFuncPtr);

            var f = table->func;
            var ft = f->type->data.func;
            var argObj = new object?[table->argsCount + 1];
            argObj[0] = orig;

            for (int i = 0; i < table->argsCount; i++)
            {
                var at = ft->args[i];
                if (at->kind != HL_type.TypeKind.HBYTES && at->kind.IsPointer())
                {
                    if (!HashlinkUtils.IsValidHLObject((void*)args[i]))
                    {
                        argObj[i + 1] = HashlinkObject.FromHashlink(HashlinkUtils.CreateDynamic(at, (void*)args[i]));
                    }
                    else
                    {
                        argObj[i + 1] = HashlinkObject.FromHashlink((HL_vdynamic*)args[i]);
                    }
                    continue;
                }
                argObj[i + 1] = HashlinkUtils.GetData(args + i, at);
            }
            try
            {
                var result = delegates[0].DynamicInvoke(argObj);
                HashlinkUtils.SetData(retVal, ft->ret, result);
            }
            catch(TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
            }
            
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct EntryItem
        {
            [FieldOffset(0)]
            public fixed byte call_code_x64[1];
            [FieldOffset(5)]
            public fixed byte call_code_x86[1];
            [FieldOffset(12)]
            public HookTable table;

        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct HookTable
        {
            //The following fields should be consistent with the hl2c_table in modcorenative.h
            public int retType; /* 2: return int64 in x86, 1: return float/double, 0: return ptr/int32/int16/byte/others */
            public void* origFuncPtr;
            public int enabled;
            public int argsCount;
            public int argBitMarks; /* bit marks : 1 means the parameter is 8 bytes long, otherwise it is 4 bytes long */
            public nint callback;

            //The following fields are not visible to the native layer

            public HL_function* func;
            public int funcIndex;
            public nint chainHandle;
        }
    }

}
