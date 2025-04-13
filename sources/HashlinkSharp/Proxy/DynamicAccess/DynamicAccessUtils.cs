using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.DynamicAccess
{
    public static class DynamicAccessUtils
    {
        [return: NotNullIfNotNull("obj")]
        public static dynamic? AsDynamic( this HashlinkObj? obj )
        {
            if (obj == null)
            {
                return null;
            }
            return HashlinkObjDynamicAccess.Create( obj );
        }
        [return: NotNullIfNotNull("obj")]
        public static dynamic? AsDynamic( object? obj )
        {
            if (obj is HashlinkObj hobj)
            {
                return hobj.AsDynamic();
            }
            return obj;
        }
    }
}
