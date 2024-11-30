using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hashlink
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public class HashlinkMetadataRef(string name, HashlinkMetadataRef.HLMType type, long data) : Attribute
    {
        public string Name => name;
        public HLMType Type => type;
        public long Data => data;
        public enum HLMType
        {
            None,
            ObjType,
            Function,
            Field
        }
    }
}
