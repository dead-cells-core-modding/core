using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime.Internals;
using HaxeProxy.Runtime.Internals.Inheritance;
using System.Reflection;

namespace HaxeProxy.Runtime
{
    public static class HaxeProxyUtils
    {
        public static void UseCSharpVersionCode( Delegate del )
        {
            UseCSharpVersionCode(del.Method);
        }
        public static void UseCSharpVersionCode( Type t )
        {
            foreach (var v in t.GetMethods(BindingFlags.Public))
            {
                if (v.GetCustomAttribute<HashlinkFIndexAttribute>() is not null &&
                    v.GetCustomAttribute<HashlinkShouldUseNativeAttribute>() is null)
                {
                    UseCSharpVersionCode(v);
                }
            }
        }
        public static void UseCSharpVersionCode( MethodInfo m )
        {
            if (m.GetCustomAttribute<HashlinkShouldUseNativeAttribute>() != null)
            {
                throw new InvalidProgramException();
            }

            if (m.GetCustomAttribute<HashlinkFIndexAttribute>() is not HashlinkFIndexAttribute fidx)
            {
                throw new InvalidOperationException();
            }

            var ifunc = HashlinkMarshal.Module.GetFunctionByFIndex(fidx.Index);
            if (ifunc is not HashlinkFunction func)
            {
                throw new InvalidOperationException();
            }

            func.UseDotnetCompiledVersion(m);
        }
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
        public static HashlinkType GetHashlinkType( Type type )
        {
            return HashlinkMarshal.GetHashlinkType(type) ??
                throw new InvalidOperationException();
        }
        public static Type GetProxyType( HashlinkType type )
        {
            return HaxeProxyManager.GetTypeFromHashlinkType(type);
        }
        public static TClass GetClass<TClass>( Type type ) where TClass : HaxeProxyBase
        {
            return ((HashlinkObjectType)GetHashlinkType(type)).GlobalValue.AsHaxe<TClass>();
        }
        public static TClass GetClass<TType, TClass>() where TClass : HaxeProxyBase
        {
            return GetClass<TClass>(typeof(TType));
        }
    }
}
