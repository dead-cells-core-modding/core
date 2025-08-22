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
        ITypeIndex
    {
        public required TypeReference TypeRef
        {
            get; set;
        }
        public List<PropertyDefinition> Fields
        {
            get;
        } = [];
        public required VirtualGroupData Group
        {
            get; set;
        }

        public int TypeIndex
        {
            get; set;
        }


        public PropertyDefinition? GetField( int index )
        {
            return Fields[index];
        }
    }
}
