using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkVirtual(HashlinkObjPtr objPtr) : HashlinkFieldObject<HL_vvirtual>(objPtr)
    {
        public HashlinkVirtual(HL_type* type) : this(HashlinkObjPtr.GetUnsafe(hl_alloc_virtual(type)))
        {

        }
    }
}
