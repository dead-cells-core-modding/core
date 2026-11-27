using Hashlink.Proxy;

namespace HaxeProxy.Runtime
{
    /// <summary>
    /// Base class for generated proxies that represent Haxe class instances.
    /// </summary>
    public class HaxeObject : HaxeProxyBase
    {
        //Never call
        private HaxeObject( HashlinkObj obj ) : base(obj)
        {
            throw new InvalidProgramException();
        }
    }
}
