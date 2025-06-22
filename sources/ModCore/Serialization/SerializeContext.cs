using dc;
using dc.haxe.io;
using dc.hxbit;
using HaxeProxy.Runtime;
using ModCore.Storage;
using MonoMod.Cil;
using MonoMod.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Serialization
{
    internal record class SerializeContext(Serializer Serializer)
    {
        private static readonly Dictionary<System.Type, FastReflectionHelper.FastInvoker> getDataInvoker = [];

        public static SerializeContext? current;
        public static readonly Stack<SerializeContext> stack = [];

        public readonly Queue<HaxeObject> queue = [];
        public readonly Dictionary<int, ItemData> items = [];
        public readonly HashSet<HaxeObject> serializedHxObj = new(ReferenceEqualityComparer.Instance);
        public bool HasData => items.Count > 0 || serializedHxObj.Count > 0 || queue.Count > 0;

        public BytesBuffer hxbitBuffer = new();

        [MemberNotNull(nameof(current))]
        public static void PushContext( SerializeContext ctx )
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
        public void AddItem( HaxeObject obj )
        {
            queue.Enqueue( obj );
        }

        public void SerializeData()
        {
            while (queue.TryDequeue(out var obj))
            {
                dynamic dyn = obj;
                var type = obj.GetType();
                if (!getDataInvoker.TryGetValue(type, out var invoker))
                {
                    invoker = type.GetInterfaces()
                        .First(i => i.IsGenericType && 
                            i.GetGenericTypeDefinition() == typeof(IHxbitSerializable<>))
                        .GetMethod("GetData")!.GetFastInvoker();
                }
                int uid = dyn.__uid;
                var data = invoker(obj)!;
                var json = JObject.FromObject(data);
                items.Add(uid, new()
                {
                    objType = type,
                    jobject = json
                });
            }
        }

        public Data Finish()
        {
            var fd = new Data();
            foreach ((var index, var data) in items)
            {
                data.jobject["__class__name__"] = data.objType.AssemblyQualifiedName;
                fd.extraData[index] = data.jobject;
            }
            fd.extraHxObjCount = serializedHxObj.Count;
            return fd;
        }

        public class ItemData
        {
            public System.Type objType = null!;
            public JObject jobject = null!;
        }

        public class Data
        {
            public Dictionary<int, JToken> extraData = [];
            public int extraHxObjCount = 0;
        }

    }
}
