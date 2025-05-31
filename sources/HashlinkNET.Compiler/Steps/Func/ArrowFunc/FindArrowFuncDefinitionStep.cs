using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func.ArrowFunc
{
    internal class FindArrowFuncDefinitionStep : ParallelCompileStep<HlFunction>
    {
        protected override void Execute( IDataContainer container, HlFunction item, int index )
        {

            var type = (HlTypeWithFun)item.Type.Value;
            var desc = type.FunctionDescription;
            if (desc.Arguments.Length == 0)
            {
                return;
            }
            var firstArg = desc.Arguments[0].Value;
            if (!container.TryGetData<ArrowFuncContextData>(firstArg, out var ctx))
            {
                return;
            }
            var md = container.GetData<FuncData>(item);
            ctx.Methods.Add(md);
            RunSync(() => ctx.TypeDef.Methods.Add(md.Definition));
        }

        protected override IReadOnlyList<HlFunction> GetItems( IDataContainer container )
        {
            return container.GetGlobalData<GlobalData>().Code.Functions;
        }
    }
}
