using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Steps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps
{
    internal class FindMissingFunctionStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Obj;
        }
        public override void Execute( IDataContainer container, HlCode code, 
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            var ot = (HlTypeWithObj)type;
            var info = container.GetData<ObjClassData>(type);
            var obj = ot.Obj;
            var td = info.TypeDef;

            foreach (var b in obj.Bindings)
            {
                if (b.FieldIndex == 4)
                {
                    continue;
                }
                var field = info.GetField(b.FieldIndex);
                var func = gdata.Code.GetFunctionById(b.FunctionIndex);
                if (func == null ||
                    field == null)
                {
                    continue;
                }
                var method = container.GetData<FuncData>(func).Definition;
                method.Name = field.Name;
                if (method.Parameters.Count > 0 &&
                    method.Parameters[0].ParameterType == td)
                {
                    method.Parameters.RemoveAt(0);
                    method.HasThis = true;
                    method.IsStatic = false;
                }
                else
                {
                    method.HasThis = false;
                    method.IsStatic = true;
                }
                td.Methods.Add(method);
            }
        }
    }
}
