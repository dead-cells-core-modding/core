using Hashlink;
using ModCore.Track;
using MonoMod.Cil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static MonoMod.Utils.FastReflectionHelper;

namespace ModCore.Hashlink.Hooks
{
    public unsafe class HashlinkFunc
    {
        private Delegate? next;
        private void* hlfunc;
        private HL_type* funcType;

        private static readonly ConcurrentDictionary<Type, FastInvoker> putArgInvoker = [];

        [ThreadStatic]
        private static void* func_arg_buf;
        [ThreadStatic]
        private static void** cached_args_ptr;
        private static void InitArg(out void* argPtr)
        {
            if (func_arg_buf == null || cached_args_ptr == null)
            {
                func_arg_buf = NativeMemory.AlignedAlloc(16 * 8, 8);
                cached_args_ptr = (void**) NativeMemory.AlignedAlloc((nuint)(16 * sizeof(void*)), (nuint) sizeof(void*));
            }
            argPtr = func_arg_buf;
        }

        private static void PutArg<T>(T val, ref void* ptr)
        {
            if(typeof(T) == typeof(int) ||
                typeof(T) == typeof(short) || 
                typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(uint) ||
                typeof(T) == typeof(ulong) ||
                typeof(T) == typeof(double) ||
                typeof(T) == typeof(byte) ||
                typeof(T) == typeof(sbyte) ||
                typeof(T) == typeof(char) ||
                typeof(T) == typeof(nint) ||
                typeof(T) == typeof(void*)
                )
            {
                Unsafe.AsRef<T>(ptr) = val;
                ptr = (void*)((nint)ptr + 8);
            }
            else if(typeof(T) == typeof(float))
            {
                PutArg((double)Unsafe.As<T, float>(ref val), ref ptr);
            }
            else if(val is HashlinkObject obj)
            {
                PutArg((nint)obj.HashlinkValue, ref ptr);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private object? CallInternal()
        {
            HL_vdynamic result = new()
            {
                type = funcType->data.func->ret
            };
            MixTrace.MarkEnteringHL();
            var ptrResult = Native.callback_c2hl(hlfunc, funcType, cached_args_ptr, &result);
            var retKind = result.type->kind;
            if(retKind.IsPointer())
            {
                var dyn = HashlinkNative.hl_alloc_dynamic(result.type);
                dyn->val.ptr = ptrResult;
                return HashlinkObject.FromHashlink(dyn);
            }
            else
            {
                return HashlinkUtils.GetData(&result.val, result.type);
            }
        }

        public HashlinkFunc(Delegate next)
        {
            this.next = next;
        }
        public HashlinkFunc(HL_function* func)
        {
            funcType = func->type;
            if (funcType->kind != HL_type.TypeKind.HFUN)
            {
                throw new InvalidOperationException();
            }

            hlfunc = HashlinkUtils.GetFunctionNativePtr(func);
        }

        public void* FuncPointer => hlfunc;
        public HL_type* FuncType => funcType;

        public object? Call()
        {
            if(next != null)
            {
                return CallDynamic([]);
            }
            InitArg(out _);

            return CallInternal();
        }
        public object? CallDynamic(params object?[] args)
        {
            if(next != null)
            {
                return next.DynamicInvoke(args);
            }
            InitArg(out var arg);
            nint narg = (nint)arg;
            foreach(var v in args)
            {
                var t = v?.GetType() ?? typeof(nint);
                var invoker = putArgInvoker.GetOrAdd(t, type =>
                {
                    return typeof(HashlinkFunc).GetMethod(nameof(PutArg), System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Static)!.MakeGenericMethod(t).GetFastInvoker();
                });
                invoker(null, v, (nint)Unsafe.AsPointer(ref narg));
            }
            return CallInternal();
        }
        public object? Call<T1>(T1 arg1)
        {
            if (next != null)
            {
                return CallDynamic(arg1);
            }
            InitArg(out var arg);

            PutArg(arg1, ref arg);

            return CallInternal();
        }
        public object? Call<T1, T2>(T1 arg1, T2 arg2)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2);
            }
            InitArg(out var arg);

            PutArg(arg1, ref arg);
            PutArg(arg2, ref arg);

            return CallInternal();
        }
        public object? Call<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3);
            }
            InitArg(out var arg);

            PutArg(arg1, ref arg);
            PutArg(arg2, ref arg);
            PutArg(arg3, ref arg);

            return CallInternal();
        }
        public object? Call<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3, arg4);
            }
            InitArg(out var arg);

            PutArg(arg1, ref arg);
            PutArg(arg2, ref arg);
            PutArg(arg3, ref arg);
            PutArg(arg4, ref arg);

            return CallInternal();
        }
        public object? Call<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3, arg4, arg5);
            }
            InitArg(out var arg);

            PutArg(arg1, ref arg);
            PutArg(arg2, ref arg);
            PutArg(arg3, ref arg);
            PutArg(arg4, ref arg);
            PutArg(arg5, ref arg);

            return CallInternal();
        }
        public object? Call<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3, arg4, arg5, arg6);
            }
            InitArg(out var arg);

            PutArg(arg1, ref arg);
            PutArg(arg2, ref arg);
            PutArg(arg3, ref arg);
            PutArg(arg4, ref arg);
            PutArg(arg5, ref arg);
            PutArg(arg6, ref arg);

            return CallInternal();
        }
        public object? Call<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            InitArg(out var arg);

            PutArg(arg1, ref arg);
            PutArg(arg2, ref arg);
            PutArg(arg3, ref arg);
            PutArg(arg4, ref arg);
            PutArg(arg5, ref arg);
            PutArg(arg6, ref arg);
            PutArg(arg7, ref arg);

            return CallInternal();
        }
        public object? Call<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7,
            T8 arg8)
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            InitArg(out var arg);

            PutArg(arg1, ref arg);
            PutArg(arg2, ref arg);
            PutArg(arg3, ref arg);
            PutArg(arg4, ref arg);
            PutArg(arg5, ref arg);
            PutArg(arg6, ref arg);
            PutArg(arg7, ref arg);
            PutArg(arg8, ref arg);

            return CallInternal();
        }
    }
}
