using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkBytes(HashlinkObjPtr objPtr) : HashlinkTypedValue<nint>(objPtr)
    {
        public HashlinkBytes() : this(HashlinkObjPtr.GetUnsafe(hl_alloc_dynamic(InternalTypes.hlt_bytes)))
        {

        }
        public HashlinkBytes(byte* ptr) : this()
        {
            ByteValue = ptr;
        }
        public HashlinkBytes(string str) : this((byte*)hl_gc_alloc_gen(InternalTypes.hlt_bytes, Encoding.Unicode.GetByteCount(str) + 2, 
            HL_Alloc_Flags.MEM_KIND_NOPTR | HL_Alloc_Flags.MEM_ZERO))
        {
            Encoding.Unicode.GetBytes(str, new Span<byte>(ByteValue, Encoding.Unicode.GetByteCount(str)));
        }
        public byte* ByteValue
        {
            get => (byte*)TypedValue;
            set => TypedValue = (nint)value;
        }
    }
}
