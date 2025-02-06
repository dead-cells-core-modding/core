using Hashlink.Reflection.Types;
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
        public HashlinkAbstract( HashlinkAbstractType type ) : this(HashlinkObjPtr.GetUnsafe(
           hl_alloc_dynamic(type.NativeType)
           ))
        {

        }
        public HashlinkAbstract( HashlinkAbstractType type, nint value ) : this(type)
        {
            Value = value;
        }
    }
}
