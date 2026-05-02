using HashlinkNET.Bytecode;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface IGlobalValueSetter
    {
        TypeDefinition? GlobalClassType {
            get; set;
        }
        PropertyDefinition? GlobalClassProp {
            get; set;
        }
        FieldDefinition? GlobalClassField {
            get; set;
        }
        HlTypeWithObj? GlobalHlType {
            get; set;
        }
    }
}
