using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCCMTool
{
    internal static class Utils
    {
        public static JToken Get(this JToken token, object propertyName)
        {
            return token[propertyName] 
                ?? throw new MissingMemberException($"Property '{propertyName}' not found in JSON token.");
        }
        public static string GetString(this JToken token, object propertyName)
        {
            var value = token[propertyName] ?? throw new MissingMemberException($"Property '{propertyName}' not found in JSON token.");
            return value?.ToString() ?? "";
        }
        public static T Get<T>(this JToken token, object propertyName)
        {
            var value = token[propertyName];
            if (value == null)
            {
                throw new MissingMemberException($"Property '{propertyName}' not found in JSON token.");
            }
            return value.ToObject<T>() 
                ?? throw new InvalidCastException($"Property '{propertyName}' could not be converted to type {typeof(T).FullName}.");
        }
    }
}
