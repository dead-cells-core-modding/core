using Hashlink.Proxy;
using Hashlink.Proxy.Objects;

namespace HaxeProxy.Runtime
{
    public class HaxeVirtual : HaxeProxyBase
    {
        //Never call
        internal HaxeVirtual( HashlinkObj obj ) : base(obj)
        {
            throw new InvalidProgramException();
        }

        public new T AsObject<T>() where T : HaxeObject
        {
            return ((HashlinkVirtual)HashlinkObj).GetValue()?.AsHaxe<T>() ?? throw new InvalidCastException();
        }
    }
}
