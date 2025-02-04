using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Types.Special
{
    internal unsafe class HashlinkStringType(HashlinkModule module, HL_type* type) : HashlinkObjectType(module, type)
    {
        public override HashlinkObj CreateInstance()
        {
            return new HashlinkString();
        }
    }
}
