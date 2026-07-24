using Hashlink;
using Hashlink.Events.Interfaces;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Values;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime.Internals.Inheritance;
using ModCore.Collections;
using ModCore.Events;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HaxeProxy.Runtime.Internals
{
    internal static unsafe class HaxeProxyManager
    {
        private class EventReceiver : IEventReceiver,
            IOnResolveHashlinkType
        {
            public EventResult<HashlinkType> OnResolveHashlinkType( Type type )
            {
                if (!type2typeId.TryGetValue(type, out var typeIndex))
                {
                    typeIndex = type.GetCustomAttribute<HashlinkTIndexAttribute>(false)?.Index ?? -1;
                }
                if (typeIndex >= 0)
                {
                    return HashlinkMarshal.Module.PreferTypes[typeIndex];
                }

                if (type.GetCustomAttribute<HashlinkTIndexAttribute>(true) != null)
                {
                    InheritanceManager.Check(type, null, out var cht);
                    return cht.Type;
                }
                return default;
            }
        }
        private static readonly delegate*< HaxeProxyBase, HashlinkObj, void > baseCtor =
            (delegate*< HaxeProxyBase, HashlinkObj, void >)
                typeof(HaxeProxyBase).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).First().MethodHandle.GetFunctionPointer();
        public static ImmutableHashSet<Type> knownProxyTypes = [];
        public static readonly Dictionary<Type, int> type2typeId = [];
        public static Type[] bindingTypes = [];
        public static Assembly? proxyAssembly;
        private static ImmutableDictionary<int, Type> subTypes = ImmutableDictionary<int, Type>.Empty;



        public static void Initialize( Assembly proxyAssembly )
        {
            HaxeProxyManager.proxyAssembly = proxyAssembly;

            bindingTypes = new Type[HashlinkMarshal.Module.PreferTypes.Length];
            var types = proxyAssembly.GetCustomAttributes<HaxeProxyBindingAttribute>();
            var subTypes = new Dictionary<int, Type>();
            foreach (var v in types)
            {
                if ((v.TypeIndex & 0x80000000) == 0)
                {
                    bindingTypes[v.TypeIndex] = v.Type;
                    if (v.Type == typeof(nint))
                    {
                        continue;
                    }
                    type2typeId[v.Type] = v.TypeIndex;
                }
                else
                {
                    subTypes[v.TypeIndex] = v.Type;
                }
            }

            type2typeId.Add(typeof(nint), HashlinkMarshal.Module.KnownTypes.Bytes.TypeIndex);

            HaxeProxyManager.subTypes = subTypes.ToImmutableDictionary();

            knownProxyTypes = [.. bindingTypes];

            EventSystem.AddReceiver(new EventReceiver());

            var real = HashlinkMarshal.Module.KnownTypes.I32;
            var fake = HashlinkMarshal.Module.PreferTypes[type2typeId[typeof(int)]];
            Debug.Assert(HashlinkMarshal.Module.PreferTypes[type2typeId[typeof(int)]].TypeKind == TypeKind.HI32);
            Debug.Assert(HashlinkMarshal.Module.PreferTypes[type2typeId[typeof(int?)]].TypeKind == TypeKind.HNULL);
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
            if (ht.TypeKind == TypeKind.HDYNOBJ)
            {
                type = typeof(HaxeDynObj);
            }
            else if (ht.TypeIndex >= 0)
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

            //if (ht is CustomHaxeType.ReflectType rt)
            //{
            //    if (!obj.isChangedTypeInfo)
            //    {
            //        throw new InvalidOperationException();
            //    }
            //}

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
