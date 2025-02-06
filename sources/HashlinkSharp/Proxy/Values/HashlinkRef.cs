using Hashlink.Marshaling;
using Hashlink.Reflection.Types;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkRef( HashlinkObjPtr objPtr ) : HashlinkTypedValue<nint>(objPtr)
    {
        public HashlinkRef( HashlinkRefType type ) : this(HashlinkObjPtr.GetUnsafe(hl_alloc_dynamic(type.NativeType)))
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
