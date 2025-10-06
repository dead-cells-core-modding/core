using dc;
using Hashlink.Proxy.Objects;
using HaxeProxy.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Utitities
{
    public static class HaxeUtils
    {
        public static dc.String AsHaxeString( this string str )
        {
            var s = new HashlinkString(str).AsHaxe<dc.String>();
            return s;
        }
    }
}
