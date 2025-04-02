using Hashlink.Marshaling;
using Hashlink.Reflection.Types;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkArray( HashlinkObjPtr objPtr ) : HashlinkTypedObj<HL_array>(objPtr)
    {
        public HashlinkArray( HashlinkType elementType, int size ) : 
            this(HashlinkObjPtr.GetUnsafe(hl_alloc_array(elementType.NativeType, size)))
        {

        }
        private HashlinkType? cachedElementType;
        public HashlinkType ElementType => cachedElementType ??= HashlinkMarshal.Module.GetMemberFrom<HashlinkType>(NativeElementType);
        public HL_type* NativeElementType => TypedRef->at;
        public int ElementSize => hl_type_size(NativeElementType);
        public int Count => TypedRef->size;
        public void* Data => TypedRef + 1;

        public object? this[int index]
        {
            get
            {
                CheckValidity();
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count, nameof(index));
                return HashlinkMarshal.ReadData((void*)((nint)Data + (ElementSize * index)), ElementType);
            }
            set
            {
                CheckValidity();
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count, nameof(index));
                HashlinkMarshal.WriteData((void*)((nint)Data + (ElementSize * index)), value, ElementType);
            }
        }
    }
}
