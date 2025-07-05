using dc.hxbit;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using HaxeProxy.Runtime;
using ModCore.Storage;
using MonoMod.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static ModCore.Serialization.SerializeContext;

namespace ModCore.Serialization
{
    internal unsafe record class DeserializeContext( Serializer Serializer )
    {
        private static readonly Dictionary<System.Type, SetDataInvokerInfo[]> setDataInvoker = [];

        public record class SetDataInvokerInfo(FastReflectionHelper.FastInvoker Invoker, Type Data);
        public static DeserializeContext? current;
        public static readonly Stack<DeserializeContext> stack = [];

        public readonly Dictionary<int, HaxeObject> hxbitObjects = [];


        [MemberNotNull(nameof(current))]
        public static void PushContext( DeserializeContext ctx )
        {
            if (current != null)
            {
                stack.Push(current);
            }
            current = ctx;
        }
        public static void PopContext()
        {
            if (stack.Count == 0)
            {
                current = null;
                return;
            }
            current = stack.Pop();
        }

        private unsafe int ReadUID()
        {
            var s = (byte*)Serializer.input.b + Serializer.inPos;
            if (*s == 0x80)
            {
                return *(int*)(s + 1);
            }
            return *s;
        }
        public void Begin( Data obj )
        {
            for (int i = 0; i < obj.extraHxObjCount; i++)
            {
#if DEBUG
                var uid = ReadUID();
                Debug.Assert(Serializer.refs.exists(uid));
#endif
                var o = Serializer.getAnyRef();
                hxbitObjects.Add(o.__uid, o.AsObject<HaxeObject>());
            }

            foreach ((var uid, var data) in obj.extraData)
            {
                var type = Type.GetType(data["__class__name__"]!.ToString(), true)!;
                var hlt = HaxeProxyUtils.GetHashlinkType(type);
                var inst = (HashlinkObject)Serializer.refs.get(uid)!;
                inst.RefreshTypeInfo(hlt.NativeType, true);
                hxbitObjects[uid] = inst.AsHaxe<HaxeObject>();
            }

            foreach ((var uid, var data) in obj.extraData)
            {
                var inst = hxbitObjects[uid];
                var type = inst.GetType();
                if (!setDataInvoker.TryGetValue(type, out var invokers))
                {
                    invokers = [.. type.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IHxbitSerializable<>))
                        .Select(x => x.GetMethod("SetData")!)
                        .Select(x => 
                            new SetDataInvokerInfo(
                                x.GetFastInvoker(),
                                x.GetParameters()[0].ParameterType
                            ))];
                }
                foreach (var v in invokers)
                {
                    var dtn = v.Data.AssemblyQualifiedName!;
                    var item = data[dtn];
                    if (item != null)
                    {
                        var d = item.ToObject(v.Data);
                        v.Invoker(inst, d);
                    }
                    else
                    {
                        v.Invoker(inst, Activator.CreateInstance(type));
                    }
                }
                if (inst is IHxbitSerializeCallback cb)
                {
                    cb.OnAfterDeserializing();
                }
            }
        }
    }
}
