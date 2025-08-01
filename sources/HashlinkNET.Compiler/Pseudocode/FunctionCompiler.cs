using BytecodeMapping;
using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.Steps;
using HashlinkNET.Compiler.Pseudocode.Steps.Backend;
using HashlinkNET.Compiler.Pseudocode.Steps.DFA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode
{
    internal class FunctionCompiler(
        HlFunction function,
        FuncData func,
        IDataContainer parent,
        BytecodeMappingData.FunctionData mappingData) : BaseCompiler
    {
        protected override void InstallSteps()
        {
            var config = parent.GetGlobalData<GlobalData>().Config;
            AddStep<GenerateFuncRegsStep>();
            AddStep<SplitBasicBlocksStep>();

            AddStep<ParseOpCodesStep>();

            //DFA
            AddStep<GenerateFlatIRStep>();
            AddStep<ScanRegistersAccessStep>();

            //Backend
            AddStep<PostprocessBasicBlocksStep>();
            AddStep<LinearizeBasicBlocksStep>();
            AddStep<EmitILStep>();
            AddStep<TrimILStep>();
            if (config.GenerateBytecodeMapping)
            {
                AddStep<FillBytecodeMappingDataStep>();
            }
            AddStep<OptimizeILStep>();
        }

        protected override void BeforeRun()
        {
            data.Parent = parent;
            data.AddGlobalData<FuncEmitGlobalData>(new(
                function,
                ((HlTypeWithFun)function.Type.Value).FunctionDescription,
                func.Definition,
                mappingData
                ));
        }
    }
}
