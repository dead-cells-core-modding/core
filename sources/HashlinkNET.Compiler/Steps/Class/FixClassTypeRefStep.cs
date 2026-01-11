using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Steps.Class
{
    internal class FixClassTypeRefStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Obj;
        }
        public override void Execute( IDataContainer container, HlCode code, 
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            var info = container.GetData<ObjClassData>(type);
            var td = info.TypeDef;
            var ot = (HlTypeWithObj)type;
            
            var obj = ot.Obj;

            TypeReference baseType;
            if (obj.Super != null)
            {
                info.Super = container.GetData<ObjClassData>(obj.Super.Value);
                baseType = container.GetTypeRef(obj.Super.Value);
            }
            else
            {
                baseType = rdata.objectBaseType;
            }
            td.BaseType = baseType;
        }
    }
}
