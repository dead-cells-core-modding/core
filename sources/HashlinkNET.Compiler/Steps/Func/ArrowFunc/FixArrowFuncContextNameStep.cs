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
            if (data.DirectParent != null)
            {
                td.Name = data.DirectParent.Definition.Name + "Context_" + type.TypeIndex;
            }
            
        }
    }
}
