using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;

namespace HashlinkNET.Compiler.Pseudocode.Steps
{
    internal class GenerateFlatIRStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();

            foreach (var v in gdata.IRBasicBlocks)
            {
                v.GenerateFlatIR();
            }
        }
    }
}
