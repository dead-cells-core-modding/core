using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data.Interfaces;
using Mono.Cecil;

#nullable disable

namespace HashlinkNET.Compiler.Data
{
    internal class EnumClassData :
        ITypeDefinitionValue,
        ITypeReferenceValue,
        IGlobalValue,
        IGlobalValueSetter,
        ITypeIndex
    {
        public TypeDefinition IndexType {
            get; set;
        } = null;
        public TypeDefinition[] ItemTypes {
            get; set;
        }
        public MethodReference[] ItemCtors {
            get; set;
        }

        public List<int> GlobalValueIndex {
            get; set;
        } = [];
        public required TypeDefinition TypeDef {
            get; set;
        }
        public TypeReference TypeRef => TypeDef;

        public TypeDefinition GlobalClassType {
            get; set;
        }

        public PropertyDefinition GlobalClassProp {
            get; set;
        }

        public FieldDefinition GlobalClassField {
            get; set;
        }

        public int TypeIndex {
            get; set;
        }
        public HlTypeWithObj GlobalHlType {
            get; set;
        }
    }
}
