using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using System.Collections;
using System.Diagnostics;
using System.Dynamic;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkArray( HashlinkObjPtr objPtr ) : 
        HashlinkTypedObj<HL_array>(objPtr)
    {
        public HashlinkArray( HashlinkType elementType, int size ) : 
            this(HashlinkObjPtr.Get(hl_alloc_array(elementType.NativeType, size)))
        {
            Debug.Assert(Handle != null || elementType.IsDyn || size == 0);
        }
        private HashlinkType? cachedElementType;
        public HashlinkType ElementType => cachedElementType ??= HashlinkMarshal.Module.GetMemberFrom<HashlinkType>(NativeElementType);
        public HL_type* NativeElementType => TypedRef->at;
        public int ElementSize => hl_type_size(NativeElementType);
        public int Count => TypedRef->size;
        public void* Data => TypedRef + 1;

        public Span<T> AsSpan<T>() where T : unmanaged
        {
            return new Span<T>(Data, ElementSize * Count / sizeof(T));
        }

        public object? this[int index]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count, nameof(index));
                return HashlinkMarshal.ReadData((void*)((nint)Data + (ElementSize * index)), ElementType);
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count, nameof(index));
                HashlinkMarshal.WriteData((void*)((nint)Data + (ElementSize * index)), value, ElementType);
            }
        }

        public override bool TryGetIndex( GetIndexBinder binder, object[] indexes, out object? result )
        {
            if (indexes.Length != 1)
            {
                result = null;
                return false;
            }
            result = this[(int)indexes[0]];
            return true;
        }
        public override bool TrySetIndex( SetIndexBinder binder, object[] indexes, object? value )
        {
            if (indexes.Length != 1)
            {
                return false;
            }
            this[(int)indexes[0]] = value;
            return true;
        }
    }
}
