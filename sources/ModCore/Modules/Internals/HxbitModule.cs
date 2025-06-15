using dc;
using dc.hl;
using dc.hxbit;
using Hashlink.Proxy;
using Hashlink.Reflection.Types;
using HaxeProxy.Events;
using HaxeProxy.Runtime;
using HaxeProxy.Runtime.Internals.Inheritance;
using ModCore.Events.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules.Internals
{
    //[CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    internal unsafe class HxbitModule : CoreModule<HxbitModule>,
        IOnAdvancedModuleInitializing
    {
        public const int MAGIC_NUMBER_EXTRA_DATA = 0x45991431;
        private class ExtraData
        {
            public Dictionary<int, string> extraClassName = [];
            public Dictionary<int, object> extraData = [];
        }
        private Class? Hook_resolveClass( Func<dc.String, Class?> orig, dc.String str )
        {
            var result = orig(str);
            if (result == null)
            {
                var type = System.Type.GetType(str.ToString(), false);
                if (type != null)
                {
                    return HaxeProxyUtils.GetClass<Class>(type);
                }
            }
            return result;
        }
        void IOnAdvancedModuleInitializing.OnAdvancedModuleInitializing()
        {
            HashlinkHooks.Instance.CreateHook("$Type", "resolveClass", Hook_resolveClass);

            Hook_Serializer.end += Hook_Serializer_end;
        }

        private dc.haxe.io.Bytes Hook_Serializer_end( Hook_Serializer.orig_end orig, Serializer self )
        {
            var refs = self.refs.keys();
            var data = new ExtraData();
            while (refs.hasNext())
            {
                var idx = refs.next();
                var val = ((HashlinkObj)self.refs.get(idx)).AsObject();
                if (val.HashlinkObj.Type is CustomHaxeType.ReflectType cht)
                {
                    data.extraClassName.Add(idx, cht.Name);
                    //data.extraData.Add(idx, val);
                }
            }
            if (data.extraClassName.Count > 0)
            {
                var str = JsonConvert.SerializeObject(data);
                var strBytes = Encoding.UTF8.GetBytes(str);
                var buf = new byte[strBytes.Length + 8];
                fixed (byte* ptr = buf)
                {
                    var p = ptr;
                    *((int*)p++) = MAGIC_NUMBER_EXTRA_DATA;
                    *((int*)p++) = strBytes.Length;
                    strBytes.CopyTo(buf.AsSpan(8));
                    self.@out.__add((nint)ptr, 0, buf.Length);
                }
            }
            return orig(self);
        }
    }
}
