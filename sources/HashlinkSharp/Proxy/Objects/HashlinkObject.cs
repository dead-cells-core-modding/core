using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkObject(void* objPtr) : HashlinkFieldObject<HL_vdynamic>(objPtr)
    {
        public HL_runtime_obj* RuntimeObj => Type->data.obj->rt;
    }
}
