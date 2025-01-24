using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Values
{
    public abstract unsafe class HashlinkValue(HashlinkObjPtr val) : HashlinkTypedObj<HL_vdynamic>(val), IHashlinkValue
    {
        public abstract object? Value { get; set; }
    }
}
