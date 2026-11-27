using Hashlink.Proxy;
using Hashlink.Proxy.Objects;

namespace HaxeProxy.Runtime
{
    /// <summary>
    /// Base class for proxies that represent Haxe virtual (structural) objects.
    /// </summary>
    public class HaxeVirtual : HaxeProxyBase
    {
        //Never call
        internal HaxeVirtual( HashlinkObj obj ) : base(obj)
        {
            throw new InvalidProgramException();
        }

        /// <summary>
        /// Gets the underlying Haxe object represented by this virtual proxy.
        /// </summary>
        /// <typeparam name="T">The expected Haxe object type.</typeparam>
        /// <returns>The underlying object cast to <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidCastException">The virtual value does not contain a compatible object.</exception>
        public new T AsObject<T>() where T : HaxeObject
        {
            return ((HashlinkVirtual)HashlinkObj).GetValue()?.AsHaxe<T>() ?? throw new InvalidCastException();
        }
    }
}
