using Hashlink.Marshaling;

namespace Hashlink.Proxy
{
    public readonly unsafe struct HashlinkObjPtr
    {
        public HL_type* Type => *(HL_type**)Pointer;
        public TypeKind TypeKind => Type->kind;
        public bool IsNull => Pointer == 0;
        public nint Pointer
        {
            get;
        }

        public static HashlinkObjPtr Get( nint ptr )
        {
            return Get((void*)ptr);
        }
        public static HashlinkObjPtr Get( void* ptr )
        {
            if (ptr != null)
            {
                if (!HashlinkMarshal.IsHashlinkObject(ptr))
                {
                    throw new InvalidOperationException();
                }
            }
            return GetUnsafe((nint)ptr);
        }
        internal static HashlinkObjPtr GetUnsafe( nint ptr )
        {
            return new(ptr);
        }
        internal static HashlinkObjPtr GetUnsafe( void* ptr )
        {
            return new((nint)ptr);
        }

        private HashlinkObjPtr( nint ptr )
        {
            Pointer = ptr;
        }
    }
}
