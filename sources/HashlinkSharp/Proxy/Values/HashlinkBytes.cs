using System;
using System.Collections.Generic;
using System.Linq;
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
        public byte* ByteValue
        {
            get => (byte*)TypedRef;
            set => TypedValue = (nint)value;
        }
    }
}
