using Hashlink;
using ModCore.Track;
using MonoMod.Cil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static MonoMod.Utils.FastReflectionHelper;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ModCore.Hashlink
{
    public unsafe class HashlinkFunc
    {
        private readonly Delegate[]? next_list;
        private readonly Delegate? next;
        private readonly int next_index = -1;
        private readonly void* hlfunc;
        private readonly HL_function* hlfunction;
        private readonly HL_type* funcType;

        private static readonly ConcurrentDictionary<Type, FastInvoker> putArgInvoker = [];

        private const int HASHLINK_MAX_ARGS_COUNT = 16;
        [ThreadStatic]
        private static void* func_arg_buf;
        [ThreadStatic]
        private static void** cached_args_ptr;

        private class PutArgContext
        {
            public int args;
        }

        public bool HasThis => HashlinkUtils.HasThis(hlfunction);

        private void InitArg(out PutArgContext ctx)
        {
            if (func_arg_buf == null || cached_args_ptr == null)
            {
                func_arg_buf = NativeMemory.AlignedAlloc(HASHLINK_MAX_ARGS_COUNT * 8, 8);
                cached_args_ptr = (void**)NativeMemory.AlignedAlloc((nuint)(HASHLINK_MAX_ARGS_COUNT * sizeof(void*)), (nuint)sizeof(void*));
            }
            for (int i = 0; i < HASHLINK_MAX_ARGS_COUNT; i++)
            {
                cached_args_ptr[i] = (void*)((nint)func_arg_buf + 8 * i);
            }
            ctx = new();
        }

        private void PutArg<T>(T val, PutArgContext ctx)
        {
            ref int idx = ref ctx.args;
            
            if (typeof(T) == typeof(int) ||
                typeof(T) == typeof(short) ||
                typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(uint) ||
                typeof(T) == typeof(ulong) ||
                typeof(T) == typeof(double) ||
                typeof(T) == typeof(byte) ||
                typeof(T) == typeof(sbyte) ||
                typeof(T) == typeof(char)
                )
            {
                var argPtr = (void*)((nint)func_arg_buf + idx * 8);
                Unsafe.AsRef<T>(argPtr) = val;
                cached_args_ptr[idx] = argPtr;
                idx++;
            }
            else if (typeof(T) == typeof(bool))
            {
                PutArg((byte)(Unsafe.As<T, bool>(ref val) ? 1 : 0), ctx);
            }
            else if (typeof(T) == typeof(float))
            {
                PutArg((double)Unsafe.As<T, float>(ref val), ctx);
            }
            else if (typeof(T) == typeof(nint) ||
                typeof(T) == typeof(nuint) ||
                typeof(T).IsPointer)
            {
                Unsafe.AsRef<T>(cached_args_ptr + idx) = val;
                idx++;
            }
            else if (val is HashlinkObject obj)
            {
                var at = funcType->data.func->args[idx];
                PutArg((nint)obj.HashlinkObj, ctx);
            }
            else if(val is string str)
            {
                var at = funcType->data.func->args[idx];
                if(at->kind == HL_type.TypeKind.HBYTES)
                {
                    PutArg((nint)HashlinkUtils.GetHLBytesString(str)->val.ptr, ctx);
                }
                else
                {
                    PutArg(HashlinkUtils.GetHLString(str), ctx);
                }
            }
            else if(val is null)
            {
                PutArg<nint>(0, ctx);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        [WillCallHL]
        private object? CallInternal()
        {
            HL_vdynamic result = new()
            {
                type = funcType->data.func->ret
            };
            MixTrace.MarkEnteringHL();
            var ptrResult = callback_c2hl(hlfunc, funcType, cached_args_ptr, &result);
            var retKind = result.type->kind;
            if(retKind == HL_type.TypeKind.HVOID)
            {
                return null;
            }
            if(result.type == HashlinkUtils.HLType_String)
            {
                _ = 1;
            }
            if (retKind.IsPointer())
            {
                if (retKind == HL_type.TypeKind.HBYTES)
                {
                    var dyn = hl_alloc_dynamic(result.type);
                    dyn->val.ptr = ptrResult;
                    return HashlinkObject.FromHashlink(dyn);
                }
                else
                {
                    return HashlinkObject.FromHashlink((HL_vdynamic*) ptrResult);
                }
            }
            else
            {
                return HashlinkUtils.GetData(&result.val, result.type);
            }
        }
        internal HashlinkFunc(Delegate[] next, HL_function* func, void* ptr = null) : this(next, 0, func, ptr)
        {

        }
        internal HashlinkFunc(Delegate[] next, int index, HL_function* func, void* ptr = null) : this(func, ptr)
        {
            next_list = next;
            next_index = index;

            if (next_index < next.Length)
            {
                this.next = next_list[next_index];
            }
        }
        public HashlinkFunc(HL_function* func, void* ptr = null)
        {
            hlfunction = func;
            funcType = func->type;
            if (funcType->kind != HL_type.TypeKind.HFUN)
            {
                throw new InvalidOperationException();
            }
            if (ptr == null)
            {
                hlfunc = HashlinkUtils.GetFunctionNativePtr(func);
            }
            else
            {
                hlfunc = ptr;
            }
        }


        public void* FuncPointer => hlfunc;
        public HL_type* FuncType => funcType;

        public object? Call()
        {
            if (next != null)
            {
                return CallDynamic([]);
            }
            InitArg(out _);

            return CallInternal();
        }
        [StackTraceHidden]
        public object? CallDynamic(params object?[]? args)
        {
            if (next != null)
            {
                return next.DynamicInvoke([new HashlinkFunc(next_list!, next_index + 1, hlfunction, FuncPointer), .. args]);
            }
            InitArg(out var arg);
            if (args != null)
            {
                foreach (var v in args)
                {
                    var t = v?.GetType() ?? typeof(nint);
                    var invoker = putArgInvoker.GetOrAdd(t, type =>
                    {
                        return typeof(HashlinkFunc).GetMethod(nameof(PutArg), System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance)!.MakeGenericMethod(t).GetFastInvoker();
                    });
                    invoker(this, v, arg);
                }
            }
            return CallInternal();
        }
        [StackTraceHidden]
        public object? Call<T1>(T1 arg1)
        {
            if (next != null)
            {
                return CallDynamic(arg1);
            }
            InitArg(out var arg);

            PutArg(arg1, arg);

            return CallInternal();
        }
        [StackTraceHidden]
        public object? Call<T1, T2>(T1 arg1, T2 arg2)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2);
            }
            InitArg(out var arg);

            PutArg(arg1, arg);
            PutArg(arg2, arg);

            return CallInternal();
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3);
            }
            InitArg(out var arg);

            PutArg(arg1, arg);
            PutArg(arg2, arg);
            PutArg(arg3, arg);

            return CallInternal();
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3, arg4);
            }
            InitArg(out var arg);

            PutArg(arg1, arg);
            PutArg(arg2, arg);
            PutArg(arg3, arg);
            PutArg(arg4, arg);

            return CallInternal();
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3, arg4, arg5);
            }
            InitArg(out var arg);

            PutArg(arg1, arg);
            PutArg(arg2, arg);
            PutArg(arg3, arg);
            PutArg(arg4, arg);
            PutArg(arg5, arg);

            return CallInternal();
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3, arg4, arg5, arg6);
            }
            InitArg(out var arg);

            PutArg(arg1, arg);
            PutArg(arg2, arg);
            PutArg(arg3, arg);
            PutArg(arg4, arg);
            PutArg(arg5, arg);
            PutArg(arg6, arg);

            return CallInternal();
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            InitArg(out var arg);

            PutArg(arg1, arg);
            PutArg(arg2, arg);
            PutArg(arg3, arg);
            PutArg(arg4, arg);
            PutArg(arg5, arg);
            PutArg(arg6, arg);
            PutArg(arg7, arg);

            return CallInternal();
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7,
            T8 arg8)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            InitArg(out var arg);

            PutArg(arg1, arg);
            PutArg(arg2, arg);
            PutArg(arg3, arg);
            PutArg(arg4, arg);
            PutArg(arg5, arg);
            PutArg(arg6, arg);
            PutArg(arg7, arg);
            PutArg(arg8, arg);

            return CallInternal();
        }
    }
}
