using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Haxe.Marshaling
{
    public static unsafe class HaxeMarshal
    {
        internal static readonly ConditionalWeakTable<HashlinkObj, HaxeObjectBase> objMapping = [];
        internal static void Initialize(HL_module* module)
        {
            HashlinkMarshal.Initialize(module);
        }

        public static HaxeObjectBase ConvertHashlinkObj( HashlinkObj value )
        {
            if (objMapping.TryGetValue(value, out var obj) && obj != null)
            {
                return obj;
            }
            
            throw new NotImplementedException();
        }


        public static object? PostProcessValue( object? value )
        {

            if (value is HashlinkObj obj)
            {
                return ConvertHashlinkObj(obj);
            }
            return value;
        }
    }
}
