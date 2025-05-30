using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func.ArrowFunc
{
    internal class FixArrowFuncContextNameStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Enum;
        }
        public override void Execute( IDataContainer container, HlCode code,
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            if (!container.TryGetData<ArrowFuncContextData>(type, out var data))
            {
                return;
            }
            var td = data.TypeDef;
            var method = data.Method!;
            var parent = method.UsedBy[0];
            td.Name = parent.Item1.Definition.Name + "Context_" + parent.Item2;
        }
    }
}
