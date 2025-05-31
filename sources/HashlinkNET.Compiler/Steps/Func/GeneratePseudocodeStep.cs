using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func
{
    internal class GeneratePseudocodeStep : ParallelCompileStep<HlFunction>
    {
        protected override void Execute( IDataContainer container, HlFunction item, int index )
        {
            var fd = container.GetData<FuncData>(item);

            var compiler = new FunctionCompiler(item,
                    fd,
                    container);
            compiler.Compile();
        }

        protected override IReadOnlyList<HlFunction> GetItems( IDataContainer container )
        {
            return container.GetGlobalData<GlobalData>().Code.Functions;
        }
    }
}
