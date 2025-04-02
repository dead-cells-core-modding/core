using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Haxe.Event.Interfaces;
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
    public static unsafe class HaxeMarshal
    {
        internal static readonly ConditionalWeakTable<HashlinkObj, HaxeObjectBase> objMapping = [];
        internal static void Initialize(HL_module* module)
        {
            HashlinkMarshal.Initialize(module);
        }

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

            
            var result = EventSystem.BroadcastEvent<IOnConvertHashlinkObject, HashlinkObj, HaxeObjectBase>(value);
            if (result.HasValue)
            {
                if (result.Value == null)
                {
                    throw new InvalidOperationException();
                }
                objMapping.TryAdd(value, result.Value);
                return result.Value;
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

        public static HaxeObjectBase AsHaxe( this HashlinkObj obj )
        {
            return ConvertHashlinkObj(obj) ?? throw new InvalidOperationException();
        }
        public static HaxeObject AsHaxe( this HashlinkObject obj )
        {
            return (HaxeObject?)ConvertHashlinkObj(obj) ?? throw new InvalidOperationException();
        }
        public static HaxeClosure AsHaxe( this HashlinkClosure obj )
        {
            return (HaxeClosure?)ConvertHashlinkObj(obj) ?? throw new InvalidOperationException();
        }


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
