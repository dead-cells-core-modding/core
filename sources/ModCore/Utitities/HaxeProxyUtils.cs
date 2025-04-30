using dc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Utitities
{
    public static class HaxeProxyUtils
    {
        public static dc.String AsHaxeString( this string str )
        {
            var s = new dc.String(Unsafe.As<dc.String>(str));
            return s;
        }
    }
}
