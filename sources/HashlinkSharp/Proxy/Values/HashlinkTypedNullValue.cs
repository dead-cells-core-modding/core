using Hashlink.Marshaling;
using Hashlink.Reflection.Types;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkTypedNullValue<TValue>( HashlinkObjPtr val ) : HashlinkValue(val)
        where TValue : unmanaged
    {
        private readonly static HashlinkType nullType = HashlinkMarshal.Module.GetTypeByName($"null<" +
            InternalTypes.GetFrom(typeof(TValue))->TypeName + ">");
        public HashlinkTypedNullValue() : this(HashlinkObjPtr.GetUnsafe(
            hl_alloc_dynamic(
                 nullType.NativeType
            )))
        {

        }
        public HashlinkTypedNullValue( TValue value ) : this()
        {
            TypedValue = value;
        }
        public virtual TValue TypedValue
        {
            get
            {
                return *(TValue*)&TypedRef->val.i64;
            }
            set
            {
                *(TValue*)&TypedRef->val.i64 = value;
            }
        }

        public override object? Value
        {
            get => TypedValue; set => TypedValue = (TValue)value!;
        }
    }
}
