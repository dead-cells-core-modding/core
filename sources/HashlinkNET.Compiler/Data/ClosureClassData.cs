using HashlinkNET.Compiler.Data.Interfaces;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Data
{
    class ClosureClassData :
        IConstructable,
        IInvokable,
        ITypeReferenceValue,
        ITypeIndex
    {
        public required MethodReference Construct {
            get; set;
        }
        public required MethodReference Invoke {
            get; set;
        }

        public required TypeReference TypeRef {
            get; set;
        }

        public int TypeIndex {
            get; set;
        }
    }
}
