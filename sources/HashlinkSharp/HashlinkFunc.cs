using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using Hashlink.Trace;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static MonoMod.Utils.FastReflectionHelper;

namespace Hashlink
{
    public unsafe class HashlinkFunc
    {
        private readonly Delegate[]? next_list;
        private readonly Delegate? next;
        private readonly int next_index = -1;
        private readonly void* hlfunc;
        private readonly HL_type_func* hlfunction;

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

        private void InitArg( out PutArgContext ctx )
        {
            if (func_arg_buf == null || cached_args_ptr == null)
            {
                func_arg_buf = NativeMemory.AlignedAlloc(HASHLINK_MAX_ARGS_COUNT * 8, 8);
                cached_args_ptr = (void**)NativeMemory.AlignedAlloc((nuint)(HASHLINK_MAX_ARGS_COUNT * sizeof(void*)), (nuint)sizeof(void*));
            }
            for (var i = 0; i < HASHLINK_MAX_ARGS_COUNT; i++)
            {
                cached_args_ptr[i] = (void*)((nint)func_arg_buf + (8 * i));
            }
            ctx = new();

            if (BindingThis is not null)
            {
                PutArg(BindingThis.Value, ctx);
            }
        }

        private void PutArg<T>( T val, PutArgContext ctx )
        {
            ref var idx = ref ctx.args;

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
                var argPtr = (void*)((nint)func_arg_buf + (idx * 8));
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
            else if (val is IHashlinkPointer obj)
            {

                PutArg(obj.HashlinkPointer, ctx);
            }
            else if (val is null)
            {
                PutArg<nint>(0, ctx);
            }
            else if (val is string str)
            {
                PutArg(new HashlinkString(str), ctx);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        [WillCallHL]
        private object? CallInternal(PutArgContext ctx)
        {
            HL_vdynamic result = new()
            {
                type = hlfunction->ret
            };
            var hlf = hlfunction;
            if (IsClosure)
            {
                hlf = hlf->parent->data.func;
            }
            HL_type funcType = new()
            {
                kind = TypeKind.HFUN,
                data =
                {
                    func = hlf
                }
            };

            if (ctx.args != hlf->nargs)
            {
                throw new InvalidOperationException("Mismatched number of parameters");
            }

            MixTrace.MarkEnteringHL();
            var ptrResult = callback_c2hl(hlfunc, &funcType, cached_args_ptr, &result);
            var retType = HashlinkMarshal.Module.GetMemberFrom<HashlinkType>(result.type);
            if (retType.IsPointer)
            {
                result.val.ptr = ptrResult;
            }
            return retType.TypeKind == TypeKind.HVOID
                ? null
                :  HashlinkMarshal.ReadData(&result.val, retType);
        }
        internal HashlinkFunc( Delegate[] next, HL_type_func* func, void* ptr = null ) : this(next, 0, func, ptr)
        {

        }
        internal HashlinkFunc( Delegate[] next, int index, HL_type_func* func, void* ptr ) : this(func, ptr)
        {
            next_list = next;
            next_index = index;

            if (next_index < next.Length)
            {
                this.next = next_list[next_index];
            }
        }
        public HashlinkFunc( HL_type_func* func, void* ptr )
        {
            hlfunction = func;

            hlfunc = ptr == null ? throw new ArgumentNullException(nameof(ptr)) : ptr;
        }

        public void* FuncPointer => hlfunc;
        public HL_type_func* FuncType => hlfunction;
        public nint? BindingThis { get; set; } = null;
        public bool IsClosure => hlfunction->parent != null;
        public object? Call()
        {
            if (next != null)
            {
                return CallDynamic([]);
            }
            InitArg(out var arg);

            return CallInternal(arg);
        }
        [StackTraceHidden]
        public object? CallDynamic( params object?[]? args )
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
            return CallInternal(arg);
        }
        [StackTraceHidden]
        public object? Call<T1>( T1 arg1 )
        {
            if (next != null)
            {
                return CallDynamic(arg1);
            }
            InitArg(out var arg);

            PutArg(arg1, arg);

            return CallInternal(arg);
        }
        [StackTraceHidden]
        public object? Call<T1, T2>( T1 arg1, T2 arg2 )
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2);
            }
            InitArg(out var arg);

            PutArg(arg1, arg);
            PutArg(arg2, arg);

            return CallInternal(arg);
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3>( T1 arg1, T2 arg2, T3 arg3 )
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3);
            }
            InitArg(out var arg);

            PutArg(arg1, arg);
            PutArg(arg2, arg);
            PutArg(arg3, arg);

            return CallInternal(arg);
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3, T4>( T1 arg1, T2 arg2, T3 arg3, T4 arg4 )
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

            return CallInternal(arg);
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3, T4, T5>( T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5 )
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

            return CallInternal(arg);
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3, T4, T5, T6>( T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6 )
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

            return CallInternal(arg);
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3, T4, T5, T6, T7>( T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7 )
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

            return CallInternal(arg);
        }
        [StackTraceHidden]
        public object? Call<T1, T2, T3, T4, T5, T6, T7, T8>( T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7,
            T8 arg8 )
        {
            if (next != null)
            {
                return CallDynamic(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
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

            return CallInternal(arg);
        }
    }
}
