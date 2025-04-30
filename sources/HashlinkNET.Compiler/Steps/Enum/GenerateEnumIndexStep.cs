using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Enum
{
    internal class GenerateEnumIndexStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type is HlTypeWithEnum;
        }
        public override void Execute( IDataContainer container, HlCode code, GlobalData gdata, 
            RuntimeImports rdata, HlType type )
        {
            var te = ((HlTypeWithEnum)type).Enum;
            var ei = container.GetData<EnumClassData>(type);
            var enumType = ei.TypeDef;
            var td = new TypeDefinition("", "Indexes", TypeAttributes.Public | TypeAttributes.Sealed, rdata.enumBaseType)
            {
                Fields =
                {
                    new("value__", 
                    FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName, 
                    gdata.Module.TypeSystem.Int32)
                },
                IsNestedPublic = true,
            };
            ei.IndexType = td;
            enumType.NestedTypes.Add(td);
            td.IsNestedPublic = true;
            ((GenericInstanceType) enumType.BaseType).GenericArguments.Add(enumType);
            ((GenericInstanceType) enumType.BaseType).GenericArguments.Add(td);
            for (int i = 0; i < te.Constructs.Length; i++)
            {
                var ec = te.Constructs[i];
                var fd = new FieldDefinition(ec.GetEnumItemName(), FieldAttributes.Public | 
                    FieldAttributes.Literal | 
                    FieldAttributes.Static |
                    FieldAttributes.HasDefault, td)
                {
                    Constant = i
                };
                td.Fields.Add(fd);
            }


        }
    }
}
