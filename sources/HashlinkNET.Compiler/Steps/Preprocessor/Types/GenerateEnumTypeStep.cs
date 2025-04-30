using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
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
        private int unnamedEnumCount = 0;
        public override void Execute( IDataContainer container,
            HlCode code, GlobalData gdata,
            RuntimeImports rdata, HlType type )
        {
            var enumType = (HlTypeWithEnum)type;

            if (!Utils.ParseHlTypeName(enumType.Enum.Name, out var np, out var name))
            {
                name = "UnnamedEnum" + Interlocked.Increment(ref unnamedEnumCount);
            }
            var td = new TypeDefinition(np, name, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract)
            {
                BaseType = new GenericInstanceType(rdata.enumType)
            };
            addedTypes.Add(new(td, type.TypeIndex));
            container.AddData(type, td, new EnumClassData()
            {
                TypeDef = td,
                TypeIndex = type.TypeIndex
            });
        }
    }
}
