using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.DynamicAccess
{
    public static class DynamicAccessUtils
    {
        [return: NotNullIfNotNull(nameof(obj))]
        public static dynamic? AsDynamic( this HashlinkObj? obj )
        {
            if (obj is IDynamicMetaObjectProvider dmop)
            {
                return dmop;
            }
            if (obj == null)
            {
                return null;
            }
#pragma warning disable CS0612
            return HashlinkObjDynamicAccess.Create( obj );
#pragma warning restore CS0612
        }
        [return: NotNullIfNotNull(nameof(obj))]
        public static dynamic? AsDynamic( object? obj )
        {
            if (obj is IDynamicMetaObjectProvider dmop)
            {
                return dmop;
            }
            if (obj is HashlinkObj hobj)
            {
                return hobj.AsDynamic();
            }
            return obj;
        }
    }
}
