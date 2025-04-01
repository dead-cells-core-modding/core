using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using Hashlink.UnsafeUtilities;
using Hashlink.Wrapper;
using System.Diagnostics.CodeAnalysis;

namespace Hashlink.Proxy.Clousre
{
    public unsafe class HashlinkClosure( HashlinkObjPtr objPtr ) : HashlinkTypedObj<HL_vclosure>(objPtr)
    {
        private Delegate? cachedWrapper;
        private HashlinkObj? cachedThis;

        public HashlinkClosure( HL_type* funcType, void* funcPtr, void* self ) :
            this(HashlinkObjPtr.GetUnsafe(hl_alloc_closure_ptr(funcType, funcPtr, self)))
        {

        }

        public nint FunctionPtr => (nint)TypedRef->fun;

        [MemberNotNull(nameof(cachedWrapper))]
        private void CheckWrapper()
        {
            cachedWrapper ??= HashlinkWrapperFactory.GetWrapper(
                        HlFuncSign.Create(
                            (HashlinkFuncType)HashlinkMarshal.GetHashlinkType(TypedRef->type)
                            ),
                        FunctionPtr);
        }

        public object? DynamicInvoke( params ReadOnlySpan<object?> args )
        {
            CheckWrapper();
            return cachedWrapper.DynamicInvoke(TypedRef->hasValue > 0 ? [
                BindingThis,
                ..args
                ] : [..args]);
        }

        public Delegate CreateDelegate(Type type)
        {
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
            return (T)CreateDelegate(typeof(T));
        }

        public HashlinkObj? BindingThis
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
