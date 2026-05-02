using Mono.Cecil;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface ITypeReferenceValue
    {
        public TypeReference TypeRef {
            get;
        }
    }
}
