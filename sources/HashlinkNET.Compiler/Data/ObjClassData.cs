using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data.Interfaces;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IObjComparable = HashlinkNET.Compiler.Data.Interfaces.IObjComparable;

namespace HashlinkNET.Compiler.Data
{
    class ObjClassData :
        IConstructable,
        IObjComparable,
        ITypeReferenceValue,
        ITypeDefinitionValue,
        IGetField,
        IGetProto,
        IGlobalValue,
        IGlobalValueSetter,
        ITypeIndex
    {
        public ObjClassData? Super
        {
            get; set;
        }
        public required TypeReference TypeRef
        {
            get; set;
        }
        public required TypeDefinition TypeDef
        {
            get; set;
        }

        public List<PropertyDefinition> Fields
        {
            get;
        } = [];
        public Dictionary<int, FuncData> Protos
        {
            get;
        } = [];

        public MethodReference? Compare
        {
            get; set;
        }

        public new MethodReference? ToString
        {
            get; set;
        }

        public MethodReference Construct
        {
            get; set;
        } = null!;

        public TypeDefinition? GlobalClassType
        {
            get; set;
        }

        public PropertyDefinition? GlobalClassProp
        {
            get; set;
        }

        public FieldDefinition? GlobalClassField
        {
            get; set;
        }

        public HlFunction? InstanceCtor
        {
            get; set;
        }
        public int TypeIndex
        {
            get; set;
        }
        public HlTypeWithObj? GlobalHlType
        {
            get; set;
        }

        private PropertyDefinition? GetFieldImpl( ref int id )
        {
            if (Super != null)
            {
                var result = Super.GetFieldImpl(ref id);
                if (result != null)
                {
                    return result;
                }
            }
            if (id >= Fields.Count)
            {
                id -= Fields.Count;
                return null;
            }
            return Fields[id];
        }
        public PropertyDefinition? GetField( int index )
        {
            return GetFieldImpl(ref index);
        }

        public MethodReference GetProto( int index )
        {
            return Protos.TryGetValue(index, out var result) ? result.Definition :
                (Super == null ? throw new InvalidOperationException() :
                Super.GetProto(index));
        }
    }
}
