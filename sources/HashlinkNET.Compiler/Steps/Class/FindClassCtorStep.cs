using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Class
{
    internal class FindClassCtorStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type is HlTypeWithObj obj &&
                GeneralUtils.ParseHlTypeName(obj.Name, out _, out var name) &&
                name.StartsWith('_');
        }
        public override void Execute( IDataContainer container, HlCode code, 
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            var obj = ((HlTypeWithObj)type).Obj;
            var od = container.GetData<ObjClassData>(type);
            HlFunction? f = null;
            for (int i = 0; i < obj.Bindings.Length; i += 2)
            {
                var fid = obj.Bindings[i].FieldIndex;
                var mid = obj.Bindings[i].FunctionIndex;
                if (fid == 4)
                {
                    //__constructor__
                    f = code.GetFunctionById(mid);
                    break;
                }
            }
            if (f != null)
            {
                od.InstanceCtor = f;
            }
        }
    }
}
