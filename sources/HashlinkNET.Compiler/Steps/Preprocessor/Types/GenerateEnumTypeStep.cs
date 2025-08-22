using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Types
{
    internal class GenerateEnumTypeStep : GenerateTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Enum;
        }
        public override void Execute( IDataContainer container,
            HlCode code, GlobalData gdata,
            RuntimeImports rdata, HlType type )
        {
            var enumType = (HlTypeWithEnum)type;

            var hasName = GeneralUtils.ParseHlTypeName(enumType.Enum.Name, out var np, out var name);
            var isArrowFuncCtx = false;
            if (!hasName)
            {
                name = "UnnamedEnum" + type.TypeIndex;
                if (enumType.Enum.Constructs.Length == 1 &&
                    string.IsNullOrEmpty(enumType.Enum.Constructs[0].Name))
                {
                    isArrowFuncCtx = true;
                }
            }
            var td = new TypeDefinition(np, name, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract)
            {
                BaseType = new GenericInstanceType(rdata.enumType)
            };
            addedTypes.Add(new(td, AddTypeKind.AddToModule | AddTypeKind.AddToTypesList, type.TypeIndex));

            if (isArrowFuncCtx)
            {
                container.AddData(type, td, new ArrowFuncContextData()
                {
                    TypeDef = td,
                    TypeIndex = type.TypeIndex
                });
            }
            else
            {
                container.AddData(type, td, new EnumClassData()
                {
                    TypeDef = td,
                    TypeIndex = type.TypeIndex
                });
            }
        }
    }
}
