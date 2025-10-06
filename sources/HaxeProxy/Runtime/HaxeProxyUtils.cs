using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime.Internals;
using HaxeProxy.Runtime.Internals.Inheritance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public static HaxeProxyBase AsObject( this HashlinkObj obj )
        {
            if (obj is HashlinkVirtual virt)
            {
                return virt.GetValue()?.AsHaxe() ?? throw new InvalidCastException();
            }
            return obj.AsHaxe();
        }
        public static T AsHaxe<T>( this HashlinkObj obj ) where T : HaxeProxyBase
        {
            return (T)obj.AsHaxe();
        }
        public static HashlinkObjectType GetHashlinkType( Type type )
        {
            var ca = type.GetCustomAttribute<HashlinkTIndexAttribute>();
            if (ca is not null)
            {
                return (HashlinkObjectType) HashlinkMarshal.Module.Types[ca.Index];
            }
            InheritanceManager.Check(type, null, out var cht);
            return cht.Type;
        }
        public static Type GetProxyType( HashlinkType type )
        {
            return HaxeProxyManager.GetTypeFromHashlinkType( type );
        }
        public static TClass GetClass<TClass>( Type type ) where TClass : HaxeProxyBase
        {
            var ca = type.GetCustomAttribute<HashlinkTIndexAttribute>();
            if (ca is not null)
            {
                return ((HashlinkObjectType)HashlinkMarshal.Module.Types[ca.Index]).GlobalValue!.AsHaxe<TClass>();
            }
            InheritanceManager.Check(type, null, out var cht);
            return cht.Data.globalValue.AsHaxe<TClass>();
        }
        public static TClass GetClass<TType, TClass>() where TClass : HaxeProxyBase
        {
            return GetClass<TClass>(typeof(TType));
        }
    }
}
