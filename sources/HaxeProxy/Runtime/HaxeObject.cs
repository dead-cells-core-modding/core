using Hashlink.Proxy;

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
