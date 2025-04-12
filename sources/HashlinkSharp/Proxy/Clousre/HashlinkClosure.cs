using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using Hashlink.UnsafeUtilities;
using Hashlink.Wrapper;
using Hashlink.Wrapper.Callbacks;
using System.Diagnostics.CodeAnalysis;

namespace Hashlink.Proxy.Clousre
{
    public unsafe class HashlinkClosure( HashlinkObjPtr objPtr ) : HashlinkTypedObj<HL_vclosure>(objPtr)
    {
        private readonly HlCallback? callback;
        private Delegate? cachedWrapper;
        private object? cachedThis;

        public HashlinkClosure( HL_type* funcType, void* funcPtr, void* self ) :
            this(HashlinkObjPtr.Get(hl_alloc_closure_ptr(funcType, funcPtr, self)))
        {

        }
        public HashlinkClosure( HashlinkFuncType funcType, nint funcPtr, nint self ) :
            this(HashlinkObjPtr.Get(self != 0 ?
                hl_alloc_closure_ptr(funcType.NativeType, (void*)funcPtr, (void*) self) :
                hl_alloc_closure_void(funcType.NativeType, (void*)funcPtr)))
        {

        }

        public HashlinkClosure( HashlinkFuncType funcType, Delegate target )
            : this(funcType, 0, 0)
        {
            callback = HlCallbackFactory.GetHlCallback(
                funcType
                );
            callback.Target = target;
            TypedRef->fun = (void*)callback.NativePointer;
            TypedRef->hasValue = 0;
        }

        public nint FunctionPtr => 
            (nint)TypedRef->fun;

        [MemberNotNull(nameof(cachedWrapper))]
        private void CheckWrapper()
        {
            cachedWrapper ??= HashlinkWrapperFactory.GetWrapper(
                            ((HashlinkFuncType)Type).BaseFunc,
                        FunctionPtr);
        }

        public object? DynamicInvoke( params object?[]? args )
        {
            CheckWrapper();
            if (callback != null)
            {
                return callback.Target!.DynamicInvoke(args);
            }
            args ??= [];
            return cachedWrapper.DynamicInvoke(
                BindingThis != null ?
                [
                    BindingThis,
                    ..args
                ] : args
                );
        }

        public Delegate CreateDelegate(Type type)
        {
            CheckWrapper();
            if (callback != null)
            {
                return callback.Target!.CreateAdaptDelegate(type);
            }
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
            return (T)CreateDelegate(typeof(T));
        }

        public object? BindingThis
        {
            get
            {
                return TypedRef->hasValue > 0 ? (
                    cachedThis ??= HashlinkMarshal.ConvertHashlinkObject(TypedRef->value)
                    ) : null;
            }
        }
    }
}
