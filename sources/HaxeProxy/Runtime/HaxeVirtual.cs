using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime
{
    public class HaxeVirtual : HaxeProxyBase
    {
        //Never call
        private HaxeVirtual( HashlinkObj obj ) : base(obj)
        {
            throw new InvalidProgramException();
        }

        public T AsObject<T>() where T : HaxeProxyBase
        {
            return ((HashlinkVirtual)HashlinkObj).GetValue()?.AsHaxe<T>() ?? throw new InvalidCastException();
        }
    }
}
