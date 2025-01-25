using Hashlink.Marshaling;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkRef( HashlinkObjPtr objPtr ) : HashlinkTypedValue<nint>(objPtr)
    {
        public HashlinkRef( HL_type* type ) : this(HashlinkObjPtr.GetUnsafe(hl_alloc_dynamic(type)))
        {

        }
        public HL_type* TargetType => TypedRef->type->data.tparam;
        public object? RefValue
        {
            get
            {
                return HashlinkMarshal.ReadData(TypedRef->val.ptr, TargetType->kind);
            }
            set
            {
                HashlinkMarshal.WriteData(TypedRef->val.ptr, value, TargetType->kind);
            }
        }
    }
}
