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
    internal class GenerateStructTypeStep : GenerateTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Struct;
        }
        public override void Execute( IDataContainer container,
            HlCode code, GlobalData gdata,
            RuntimeImports rdata, HlType type )
        {
            var objType = (HlTypeWithObj)type;

            GeneralUtils.ParseHlTypeName(objType.Obj.Name, out var np, out var name);
            var td = new TypeDefinition(np, name, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed)
            {
                IsValueType = true,
                BaseType = rdata.objBaseType
            };
            addedTypes.Add(new(td, type.TypeIndex));

            container.AddData(type, td, new ObjClassData()
            {
                TypeDef = td,
                TypeRef = td,
                TypeIndex = type.TypeIndex
            });

            throw new NotImplementedException(); //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }
    }
}
