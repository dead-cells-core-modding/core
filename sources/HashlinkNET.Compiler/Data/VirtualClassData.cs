using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data.Interfaces;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Data
{
    class VirtualClassData : 
        IGetField,
        ITypeReferenceValue,
        ITypeIndex,
        ITypeDefinitionValue
    {
        public required TypeReference TypeRef
        {
            get; set;
        }
        public List<PropertyDefinition> Fields
        {
            get;
        } = [];
        public required List<HlObjField> SortedFields
        {
            get; set;
        }

        public required string ShortName
        {
            get; set;
        }

        public required string FullName
        {
            get; set;
        }

        public int TypeIndex
        {
            get; set;
        }

        public TypeDefinition TypeDef
        {
            get; set;
        } = null!;

        public PropertyDefinition? GetField( int index )
        {
            return Fields[index];
        }
    }
}
