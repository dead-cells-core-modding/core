using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkVirtual(void* objPtr) : HashlinkFieldObject<HL_vvirtual>(objPtr)
    {

    }
}
