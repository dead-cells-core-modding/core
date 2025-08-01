using BytecodeMapping;
using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode;
using MonoMod.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func
{
    internal class GeneratePseudocodeStep : ParallelCompileStep<HlFunction>
    {
        private int processedCount = 0;
        private ConcurrentBag<BytecodeMappingData.FunctionData> mappingData = [];
        protected override void Execute( IDataContainer container, HlFunction item, int index )
        {
            var fd = container.GetData<FuncData>(item);

            var mappingData = new BytecodeMappingData.FunctionData()
            {
                FunctionIndex = item.FunctionIndex,
                Name = fd.Definition.GetID()
            };

            this.mappingData.Add(mappingData);

            var compiler = new FunctionCompiler(item,
                    fd,
                    container,
                    mappingData);
            compiler.Compile();

            if (processedCount++ > 7000)
            {
                processedCount = 0;
                GC.Collect();
            }
        }
        protected override void PostProcessing( IDataContainer container )
        {
            var gdata = container.GetGlobalData<GlobalData>();
            if (gdata.Config.GenerateBytecodeMapping)
            {
                foreach (var v in mappingData)
                {
                    gdata.BytecodeMappingData.Functions.Add(v.FunctionIndex, v);
                }
            }

            mappingData.Clear();
            GC.Collect( );
        }
        protected override IReadOnlyList<HlFunction> GetItems( IDataContainer container )
        {
            return container.GetGlobalData<GlobalData>().Code.Functions;
        }
    }
}
