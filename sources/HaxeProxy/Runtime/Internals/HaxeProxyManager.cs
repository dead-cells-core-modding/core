using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Values;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime.Internals.Inheritance;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HaxeProxy.Runtime.Internals
{
    internal static unsafe class HaxeProxyManager
    {
        
        private static readonly delegate*< HaxeProxyBase, HashlinkObj, void > baseCtor =
            (delegate*< HaxeProxyBase, HashlinkObj, void > )
                typeof(HaxeProxyBase).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).First().MethodHandle.GetFunctionPointer();
        public static ImmutableHashSet<Type> knownProxyTypes = [];
        public static readonly Dictionary<Type, int> type2typeId = [];
        private static Type[] bindingTypes = [];
        private static ImmutableDictionary<int, Type> subTypes = ImmutableDictionary<int, Type>.Empty;
        

        
        public static void Initialize( Assembly proxyAssembly )
        {
            bindingTypes = new Type[HashlinkMarshal.Module.Types.Length];
            var types = proxyAssembly.GetCustomAttributes<HaxeProxyBindingAttribute>();
            var subTypes = new Dictionary<int, Type>();
            foreach (var v in types)
            {
                if ((v.TypeIndex & 0x80000000) == 0)
                {
                    bindingTypes[v.TypeIndex] = v.Type;
                    type2typeId[v.Type] = v.TypeIndex;
                }
                else
                {
                    subTypes[v.TypeIndex] = v.Type;
                }
            }
            HaxeProxyManager.subTypes = subTypes.ToImmutableDictionary();
    
            knownProxyTypes = [.. bindingTypes];
        }
        public static void CheckCustomProxy( HaxeProxyBase proxy, HashlinkObj obj )
        {
            var type = proxy.GetType();
            if (!obj.Type.IsObject || knownProxyTypes.Contains(type))
            {
                return;
            }
            obj.MarkStateful();
            InheritanceManager.Check(type, (HashlinkObjectType)obj.Type, out var cht);
            *(nint*)obj.HashlinkPointer = (nint)cht.nativeType;
            obj.RefreshTypeInfo(cht.nativeType, false);
        }
        public static Type GetTypeFromHashlinkType( HashlinkType ht, HashlinkObj? obj = null )
        {
            Type type;
            if (ht.TypeIndex >= 0)
            {
                if (ht.IsEnum && obj != null)
                {
                    var hle = (HashlinkEnum)obj;
                    type = subTypes[HaxeProxyBindingAttribute.GetSubTypeId(ht.TypeIndex,
                        hle.Index)];
                }
                else
                {
                    type = bindingTypes[ht.TypeIndex];
                }
            }
            else if (ht is CustomHaxeType.ReflectType rt)
            {
                type = rt.CustomType.Data.type;
            }
            else
            {
                throw new NotSupportedException();
            }
            return type;
        }
        public static HaxeProxyBase CreateProxy( HashlinkObj obj )
        {
            var ht = obj.Type;

            if (ht is CustomHaxeType.ReflectType rt)
            {
                if (!obj.isChangedTypeInfo)
                {
                    throw new InvalidProgramException();
                }
            }

            var type = GetTypeFromHashlinkType(ht, obj);

            Debug.Assert(type != null);
            Debug.Assert(!type.IsAbstract);

            var inst = (HaxeProxyBase)RuntimeHelpers.GetUninitializedObject(type);
            inst.createByManager = true;
            baseCtor(inst, obj);
            return inst;
        }
    }
}
