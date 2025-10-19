using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Serialization.Converters
{
    /// <summary>
    /// 
    /// </summary>
    public class HxbitConverter : JsonConverter<HaxeObject>
    {
        ///<inheritdoc/>
        public override HaxeObject? ReadJson( JsonReader reader, Type objectType, 
            HaxeObject? existingValue, bool hasExistingValue, JsonSerializer serializer )
        {
            var ctx = DeserializeContext.current ?? throw new InvalidOperationException();
            var token = JToken.ReadFrom( reader );
            if (token == null ||
                token is JValue and { Type: JTokenType.Null })
            {
                return null;
            }

            var uid = token["uid"]?.Value<int>() ??
                throw new InvalidOperationException();

            var obj = ctx.hxbitObjects[uid];
            return obj;
        }
        ///<inheritdoc/>
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
