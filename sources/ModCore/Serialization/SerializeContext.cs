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
using System.IO.Hashing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static dc.hxsl.FunctionKind;

namespace ModCore.Serialization
{
    internal unsafe record class SerializeContext(Serializer Serializer)
    {
        private static readonly Dictionary<System.Type, FastReflectionHelper.FastInvoker[]> 
            getDataInvoker = [];

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
                if (obj is IHxbitSerializeCallback cb)
                {
                    cb.OnAfterDeserializing();
                }
                var type = obj.GetType();
                if (!getDataInvoker.TryGetValue(type, out var invokers))
                {
                    invokers = [.. type.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IHxbitSerializable<>))
                        .Select(x => x.GetMethod("GetData")!.GetFastInvoker())];
                }
                var uid = dyn.__uid;
                var json = new JObject();
                
                foreach (var v in invokers)
                {
                    var data = v(obj)!;
                    json[data.GetType().AssemblyQualifiedName!] = JObject.FromObject(data);
                }
               
                
                items.Add(uid, new ItemData()
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
            fd.extraHxObjSize = hxbitBuffer.pos;
            fd.extraHxObjChecksum = Crc64.HashToUInt64(new ReadOnlySpan<byte>(
                (byte*)hxbitBuffer.b, hxbitBuffer.pos
                ));
            return fd;
        }

        public class ItemData
        {
            public System.Type objType = null!;
            public JObject jobject = null!;
        }

        public class Data
        {
            public Dictionary<int, JObject> extraData = [];
            public int extraHxObjCount = 0;
            public int extraHxObjSize = 0;
            public ulong extraHxObjChecksum = 0;
        }

    }
}
