using Hashlink.Proxy.Objects;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using ModCore.Utitities;

namespace ModCore.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public static class HaxeProxyUtils
    {
        /// <summary>
        /// Convert a string to a Haxe string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static dc.String AsHaxeString( this string str )
        {
            var s = new HashlinkString(str).AsHaxe<dc.String>();
            return s;
        }
    }
}
