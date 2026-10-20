using Hashlink.Marshaling;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Reflection.Types;
using Hashlink.UnsafeUtilities;
using Hashlink.Wrapper;
using Hashlink.Wrapper.Callbacks;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Reflection;
using System.Reflection.Emit;

namespace Hashlink.Proxy.Clousre
{
    public unsafe class HashlinkClosure( HashlinkObjPtr objPtr ) : HashlinkTypedObj<HL_vclosure>(objPtr)
    {
        protected HlCallback? callback;
        private Delegate? cachedWrapper;
        private object? cachedThis;

        private class FastInvokerCache
        {
            public required FastReflectionHelper.FastInvoker invoker;
            public required int paramCount;
        }

        private FastInvokerCache? cachedCallbackInvoker;
        private FastInvokerCache? cachedWrapperInvoker;

        public HashlinkClosure( HL_type* funcType, void* funcPtr, void* self ) :
            this(HashlinkObjPtr.Get(hl_alloc_closure_ptr(EnsureThreadRegistered(funcType), funcPtr, self)))
        {

        }
        public HashlinkClosure( HashlinkFuncType funcType, nint funcPtr, nint self ) :
            this(HashlinkObjPtr.Get(self != 0 ?
                hl_alloc_closure_ptr(EnsureThreadRegistered(funcType.BaseFunc.NativeType), (void*)funcPtr, (void*)self) :
                hl_alloc_closure_void(EnsureThreadRegistered(funcType.NativeType), (void*)funcPtr)))
        {

        }

        public HashlinkClosure( HashlinkFuncType funcType, Delegate target )
            : this(funcType, 0, 0)
        {
            callback = HlCallbackFactory.GetHlCallback(
                funcType
                );
            callback.Target = target.CreateAdaptDelegate();
            TypedRef->hasValue = 0;
            MarkStateful();
        }

        public nint FunctionPtr =>
            EnsureNativePointer();

        private nint EnsureNativePointer( HL_vclosure* native = null )
        {
            if (native == null)
            {
                native = TypedRef;
            }
            if (native->fun == null)
            {
                if (callback == null)
                {
                    throw new InvalidOperationException();
                }
                native->fun = (void*)callback.NativePointer;
            }
            return (nint)native->fun;
        }
        [MemberNotNull(nameof(cachedWrapper))]
        private void CheckWrapper()
        {
            cachedWrapper ??= HashlinkWrapperFactory.GetWrapper(
                            ((HashlinkFuncType)Type).BaseFunc,
                        FunctionPtr);
        }

        private object? FastDynamicInvoke( Delegate del, ref FastInvokerCache? cache, params object?[] args )
        {
            var method = del.Method;

            if (cache == null)
            {
                cache = new()
                {
                    invoker = FastReflectionHelper.GetFastInvoker(del.Method),
                    paramCount = method.GetParameters().Length
                };
            }

            if (method.CallingConvention.Has(CallingConventions.ExplicitThis) ||
                args.Length == cache.paramCount - 1)
            {
                return cache.invoker(null, [del.Target, .. args]);
            }

            return cache.invoker(null, args);
        }

        public object? DynamicInvoke( params object?[]? args )
        {
            if (callback != null)
            {
                args ??= [];
                return FastDynamicInvoke(callback.Target!, ref cachedCallbackInvoker, args);
            }
            CheckWrapper();
            args ??= [];
            return FastDynamicInvoke(cachedWrapper, ref cachedWrapperInvoker,
                BindingThis != null ?
                [
                    BindingThis,
                    ..args
                ] : args
                );
        }

        public Delegate CreateDelegate()
        {
            if (callback != null)
            {
                return callback.Target!;
            }
            CheckWrapper();
            if (TypedRef->hasValue > 0)
            {
                return cachedWrapper.Bind(BindingThis);
            }
            else
            {
                return cachedWrapper;
            }
        }

        public Delegate CreateDelegate( Type type )
        {
            if (callback != null)
            {
                return callback.Target!.CreateAdaptDelegate(type);
            }
            CheckWrapper();
            if (TypedRef->hasValue > 0)
            {
                return cachedWrapper.Bind(BindingThis, type);
            }
            else
            {
                return cachedWrapper.CreateAdaptDelegate(type);
            }
        }

        public T CreateDelegate<T>() where T : Delegate
        {
            return ((IExtraData)this).GetData<DelegateCache<T>>().Value;
        }

        public object? BindingThis {
            get {
                return TypedRef->hasValue > 0 ? (
                    cachedThis ??= HashlinkMarshal.ConvertHashlinkObject(TypedRef->value)
                    ) : null;
            }
        }
        public override nint HashlinkPointer {
            get {
                EnsureNativePointer((HL_vclosure*)base.HashlinkPointer);
                return base.HashlinkPointer;
            }
        }

        public HashlinkClosure NoClosure => BindingThis == null ? this : new HashlinkClosure(((HashlinkFuncType)Type).BaseFunc, FunctionPtr, 0);

        public override bool TryInvoke( InvokeBinder binder, object?[]? args, out object? result )
        {
            result = DynamicAccessUtils.AsDynamic(DynamicInvoke(args ?? []));
            return true;
        }
        public override bool TryConvert( ConvertBinder binder, out object? result )
        {
            if (binder.Type.IsAssignableTo(typeof(Delegate)))
            {
                result = CreateDelegate(binder.Type);
                return true;
            }
            return base.TryConvert(binder, out result);
        }
    }
}
