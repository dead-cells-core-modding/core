using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Serialization.Converters
{
    public class HxbitConverter : JsonConverter<HaxeObject>
    {
        public override HaxeObject? ReadJson( JsonReader reader, Type objectType, 
            HaxeObject? existingValue, bool hasExistingValue, JsonSerializer serializer )
        {
            var ctx = DeserializeContext.current ?? throw new InvalidOperationException();
            reader.Read();
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonToken.StartObject ||
                reader.ReadAsString() != "uid")
            {
                throw new InvalidOperationException();
            }
            var uid = reader.ReadAsInt32() ??
                throw new InvalidOperationException();
            reader.Read();
            var obj = ctx.hxbitObjects[uid];
            return obj;
        }

        public override void WriteJson( JsonWriter writer, HaxeObject? value, 
            JsonSerializer serializer )
        {
            if (value == null)
            {
                writer.WriteNull(); 
                return;
            }

            var ctx = SerializeContext.current ?? throw new InvalidOperationException();
            var virt = value.ToVirtual<virtual___uid_getCLID_getSerializeSchema_serialize_unserialize_unserializeInit_>();
            
            if (ctx.serializedHxObj.Add(value))
            {
                ctx.Serializer.addAnyRef(virt);
            }
            writer.WriteStartObject();
            writer.WritePropertyName("uid");
            writer.WriteValue(virt.__uid);
            writer.WriteEndObject();
        }
    }
}
