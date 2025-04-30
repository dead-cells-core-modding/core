using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Steps;
using HashlinkNET.Compiler.Steps.Class;
using HashlinkNET.Compiler.Steps.Enum;
using HashlinkNET.Compiler.Steps.Func;
using HashlinkNET.Compiler.Steps.Preprocessor.Fun;
using HashlinkNET.Compiler.Steps.Preprocessor.Imports;
using HashlinkNET.Compiler.Steps.Preprocessor.Types;
using HashlinkNET.Compiler.Steps.Virtual;
using Mono.Cecil;
using System.Collections;
using System.Diagnostics;

namespace HashlinkNET.Compiler
{
    public class HashlinkCompiler(
        HlCode code, 
        AssemblyDefinition output,
        CompileConfig? config = null) : BaseCompiler
    {
        private bool compiled = false;

        public AssemblyDefinition Output => output;
        protected override void InstallSteps()
        {
            #region Pre Process
            AddStep<ImportRuntimeTypesStep>();
            AddStep<GenerateFuncBaseTypeStep>();
            AddStep<ImportBasicHlTypesStep>();

            AddStep<GenerateClassTypeStep>();
            AddStep<GenerateEnumTypeStep>();
            AddStep<GenerateStructTypeStep>();
            AddStep<GenerateVirtualTypeStep>();

            AddStep<ImportFuncTypeStep>();
            AddStep<ImportNullTypeStep>();
            AddStep<ImportRefTypeStep>();
            #endregion
            #region Fix type ref
            AddStep<FixClassTypeRefStep>();
            AddStep<FixFuncTypeRefStep>();
            #endregion

            #region Function And Native
            AddStep<GenerateFuncDefStep>();
            #endregion

            #region Enum
            AddStep<GenerateEnumIndexStep>();
            AddStep<GenerateEnumItemTypesStep>();
            #endregion

            #region Virtual
            AddStep<FixVirtualFieldsStep>();
            #endregion

            #region Class
            AddStep<FindClassCtorStep>();
            AddStep<GenerateClassFieldsStep>();

            AddStep<GenerateClassSpecialMethodDefsStep>();

            AddStep<GenerateClassGlobalPropStep>();
            AddStep<GenerateClassMethodDefStep>();
            AddStep<GenerateClassCtorStep>();
            #endregion

        }

        public override void Compile()
        {
            if (compiled)
            {
                throw new InvalidOperationException();
            }

            InstallSteps();

            data.AddGlobalData(config ?? new());
            data.AddGlobalData(new GlobalData(
            
                Assembly: output,
                Module: output.MainModule,
                Code: code
            ));

            RunSteps();
            data.Clear();

            compiled = true;
        }
    }
}
