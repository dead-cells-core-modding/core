using Mono.Cecil;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface ITypeDefinitionValue
    {
        public TypeDefinition TypeDef {
            get;
        }
    }
}
