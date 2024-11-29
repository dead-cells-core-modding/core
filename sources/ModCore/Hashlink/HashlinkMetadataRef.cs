using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hashlink
{
    [AttributeUsage(AttributeTargets.All)]
    public class HashlinkMetadataRef(string name, HashlinkMetadataRef.HLMType type) : Attribute
    {
        public string Name => name;
        public HLMType Type => type;
        public enum HLMType
        {
            None,
            ObjType,
            Function
        }
    }
}
