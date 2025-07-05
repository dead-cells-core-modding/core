using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.Steps;
using HashlinkNET.Compiler.Pseudocode.Steps.SSA;
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
        IDataContainer parent) : BaseCompiler
    {
        protected override void InstallSteps()
        {
            AddStep<GenerateFuncRegsStep>();
            AddStep<SplitBasicBlocksStep>();

            AddStep<ParseOpCodesStep>();

            AddStep<GenerateFlatIRStep>();
            AddStep<SSAScanRegistersAccessStep>();

            AddStep<EmitILStep>();
            AddStep<OptimizeILStep>();
        }

        protected override void BeforeRun()
        {
            data.Parent = parent;
            data.AddGlobalData<FuncEmitGlobalData>(new(
                function,
                ((HlTypeWithFun)function.Type.Value).FunctionDescription,
                func.Definition
                ));
        }
    }
}
