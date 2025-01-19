using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkBytes(void* objPtr) : HashlinkTypedValue<nint>(objPtr)
    {
        public byte* ByteValue
        {
            get => (byte*)TypedRef;
            set => TypedValue = (nint)value;
        }
    }
}
