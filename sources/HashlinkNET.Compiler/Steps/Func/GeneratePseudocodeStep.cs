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
        private int processedCount = 0;
        protected override void Execute( IDataContainer container, HlFunction item, int index )
        {
            var fd = container.GetData<FuncData>(item);

            var compiler = new FunctionCompiler(item,
                    fd,
                    container);
            compiler.Compile();

            if (processedCount++ > 7000)
            {
                processedCount = 0;
                GC.Collect();
            }
        }
        protected override void PostProcessing( IDataContainer container )
        {
            GC.Collect( );
        }
        protected override IReadOnlyList<HlFunction> GetItems( IDataContainer container )
        {
            return container.GetGlobalData<GlobalData>().Code.Functions;
        }
    }
}
