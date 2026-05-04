using HashlinkNET.Bytecode;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Data
{
    internal class VirtualGroupData
    {
        public required string Name {
            get; set;
        }
        public required TypeDefinition TypeDef {
            get; set;
        }
        public List<HlTypeWithVirtual> Types {
            get; set;
        } = [];
        public List<string> SortedFieldNames {
            get; set;
        } = [];
        public HashSet<string> DifferentTypeFields {
            get; set;
        } = [];

    }
}
