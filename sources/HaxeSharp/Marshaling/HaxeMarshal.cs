using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using ModCore.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Haxe.Marshaling
{
    [Obsolete]
    public static unsafe class HaxeMarshal
    {
        [Obsolete]
        internal static readonly ConditionalWeakTable<HashlinkObj, HaxeObjectBase> objMapping = [];
        [Obsolete]
        internal static void Initialize(HL_module* module)
        {
            HashlinkMarshal.Initialize(module);
        }
        [Obsolete]
        public static HaxeObjectBase? ConvertHashlinkObj( HashlinkObj? value )
        {
            if (value == null)
            {
                return null;
            }
            
            if (objMapping.TryGetValue(value, out var obj) && obj != null)
            {
                return obj;
            }

            if (value is HashlinkClosure closure)
            {
                return new HaxeClosure(closure);
            }
            else if (value is HashlinkObject hobj)
            {
                return new HaxeObject(hobj);
            }

            return null; 
        }
        [Obsolete]
        public static HaxeObjectBase AsHaxe( HashlinkObj obj )
        {
            return ConvertHashlinkObj(obj) ?? throw new InvalidOperationException();
        }
        [Obsolete]
        public static HaxeObject AsHaxe( HashlinkObject obj )
        {
            return (HaxeObject?)ConvertHashlinkObj(obj) ?? throw new InvalidOperationException();
        }
        [Obsolete]
        public static HaxeClosure AsHaxe( HashlinkClosure obj )
        {
            return (HaxeClosure?)ConvertHashlinkObj(obj) ?? throw new InvalidOperationException();
        }

        [Obsolete]
        public static object? PostProcessValue( object? value )
        {

            if (value is HashlinkObj obj)
            {
                return ConvertHashlinkObj(obj) ?? value;
            }
            return value;
        }
    }
}
