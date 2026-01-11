using HashlinkNET.Bytecode;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface IGlobalValue
    {
        TypeDefinition? GlobalClassType
        {
            get;
        }
        PropertyDefinition? GlobalClassProp
        {
            get;
        }
        FieldDefinition? GlobalClassField
        {
            get;
        }
        HlTypeWithObj? GlobalHlType
        {
            get; 
        }
    }
}
