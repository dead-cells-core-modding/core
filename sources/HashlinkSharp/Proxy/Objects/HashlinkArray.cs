using Hashlink.Marshaling;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkArray( HashlinkObjPtr objPtr ) : HashlinkTypedObj<HL_array>(objPtr)
    {
        public HashlinkArray( HL_type* type, int size ) : this(HashlinkObjPtr.GetUnsafe(hl_alloc_array(type, size)))
        {

        }
        public HL_type* ElementType => TypedRef->at;
        public int ElementSize => hl_type_size(ElementType);
        public int Count => TypedRef->size;
        public void* Data => TypedRef + 1;

        public object? this[int index]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count, nameof(index));
                return HashlinkMarshal.ReadData((void*)((nint)Data + (ElementSize * index)), ElementType->kind);
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count, nameof(index));
                HashlinkMarshal.WriteData((void*)((nint)Data + (ElementSize * index)), value, ElementType->kind);
            }
        }
    }
}
