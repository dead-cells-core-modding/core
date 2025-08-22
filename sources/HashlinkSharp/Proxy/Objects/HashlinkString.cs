using Hashlink.Marshaling;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkString( HashlinkObjPtr objPtr ) : HashlinkObject(objPtr), IHashlinkValue
    {
        public HashlinkString() : this(
            HashlinkObjPtr.Get(
                hl_alloc_obj(HashlinkMarshal.Module.KnownTypes.String.NativeType)
                )
            )
        {
            
        }

        public override string ToString()
        {
            return TypedValue!;
        }

        public HashlinkString( string val ) : this()
        {
            TypedValue = val;
        }
        public string? TypedValue
        {
            get
            {
                return new(((HL_vstring*)HashlinkPointer)->bytes);
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value, nameof(value));
                var str = (HL_vstring*)HashlinkPointer;
                str->bytes = (char*)hl_gc_alloc_gen(InternalTypes.hlt_bytes, (value.Length * 2) + 2, HL_Alloc_Flags.MEM_KIND_NOPTR |
                    HL_Alloc_Flags.MEM_ZERO);
                str->length = value.Length;
                fixed (char* p = value)
                {
                    Buffer.MemoryCopy(p, str->bytes, value.Length * 2, value.Length * 2);
                }
            }
        }
        public object? Value
        {
            get => TypedValue; set => TypedValue = (string?)value;
        }
    }
}
