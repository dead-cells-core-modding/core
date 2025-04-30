using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Class
{
    class GenerateClassSpecialMethodDefsStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Obj || type.Kind == HlTypeKind.Struct;
        }
        public override void Execute( IDataContainer container, HlCode code, GlobalData gdata,
            RuntimeImports rdata, HlType type )
        {

            var ot = (HlTypeWithObj)type;
            var info = container.GetData<ObjClassData>(type);

            var td = info.TypeDef;
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public |
                            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                            gdata.Module.TypeSystem.Void);
            td.Methods.Add(ctor);
            info.Construct = ctor;


            var toString = td.FindMethod("toString");
            if (toString is not null)
            {
                toString.IsVirtual = true;
                toString.Name = "ToString";
                info.ToString = toString;
            }

            var compare = td.FindMethod("__compare");
            if (compare is not null)
            {
                info.Compare = compare;
            }
        }
    }
}
