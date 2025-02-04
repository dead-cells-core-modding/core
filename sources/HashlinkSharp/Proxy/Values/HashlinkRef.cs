using Hashlink.Marshaling;
using Hashlink.Reflection.Types;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkRef( HashlinkObjPtr objPtr ) : HashlinkTypedValue<nint>(objPtr)
    {
        public HashlinkRef( HL_type* type ) : this(HashlinkObjPtr.GetUnsafe(hl_alloc_dynamic(type)))
        {

        }
        public HashlinkType TargetType => ((HashlinkRefType)Type).RefType;
        public object? RefValue
        {
            get
            {
                return HashlinkMarshal.ReadData(TypedRef->val.ptr, TargetType);
            }
            set
            {
                HashlinkMarshal.WriteData(TypedRef->val.ptr, value, TargetType);
            }
        }
    }
}
