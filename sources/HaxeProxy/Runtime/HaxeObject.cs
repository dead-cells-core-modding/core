using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime
{
    public class HaxeObject : HaxeProxyBase
    {
        //Never call
        private HaxeObject( HashlinkObj obj ) : base(obj)
        {
            throw new InvalidProgramException();
        }
    }
}
