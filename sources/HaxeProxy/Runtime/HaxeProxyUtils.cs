using Hashlink.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime
{
    public static class HaxeProxyUtils
    {
        public static HaxeProxyBase AsHaxe( this HashlinkObj obj )
        {
            return ((IExtraData)obj).GetData<HaxeProxyBase>();
        }
        public static T AsHaxe<T>( this HashlinkObj obj ) where T : HaxeProxyBase
        {
            return (T)obj.AsHaxe();
        }
    }
}
