using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hashlink.HL_vdynamic;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkAbstract( HashlinkObjPtr objPtr ) : HashlinkTypedValue<nint>(objPtr)
    {
        public HashlinkAbstract() : this(HashlinkObjPtr.GetUnsafe(
           hl_alloc_dynamic(InternalTypes.hlt_abstract)
           ))
        {

        }
        public HashlinkAbstract( nint value ) : this()
        {
            Value = value;
        }
    }
}
