using BytecodeMapping;
using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Steps;
using HashlinkNET.Compiler.Steps;
using HashlinkNET.Compiler.Steps.Class;
using HashlinkNET.Compiler.Steps.Enum;
using HashlinkNET.Compiler.Steps.Func;
using HashlinkNET.Compiler.Steps.Func.ArrowFunc;
using HashlinkNET.Compiler.Steps.Func.Native;
using HashlinkNET.Compiler.Steps.Hooks;
using HashlinkNET.Compiler.Steps.Preprocessor.Fun;
using HashlinkNET.Compiler.Steps.Preprocessor.Imports;
using HashlinkNET.Compiler.Steps.Preprocessor.Native;
using HashlinkNET.Compiler.Steps.Preprocessor.Types;
using HashlinkNET.Compiler.Steps.Preprocessor.Types.Virtuals;
using HashlinkNET.Compiler.Steps.Virtual;
using Mono.Cecil;
using System.Xml.Linq;

namespace HashlinkNET.Compiler
{
    public class HashlinkCompiler(
        HlCode code, 
        AssemblyDefinition output,
        CompileConfig? config = null) : BaseCompiler
    {
        public CompileConfig Config
        {
            get;
        } = config ?? new();
        public AssemblyDefinition Output => output;
        public BytecodeMappingData BytecodeMappingData
        {
            get;
        } = new();
        public XDocument XmlDocument
        {
            get;
        } = new();
        protected override void InstallSteps()
        {
            #region Pre Process
            AddStep<ImportRuntimeTypesStep>();
            AddStep<GenerateFuncBaseTypeStep>();
            AddStep<ImportBasicHlTypesStep>();

            AddStep<GenerateClassTypeStep>();
            AddStep<GenerateEnumTypeStep>();
            AddStep<GenerateStructTypeStep>();

            AddStep<ScanVirtualTypeStep>();
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

            AddStep<FindClosureUsedByStep>();
            #endregion

            #region Enum
            AddStep<GenerateEnumIndexStep>();
            AddStep<GenerateEnumItemTypesStep>();
            #endregion

            #region Virtual
            AddStep<FixVirtualGenericTypeStep>();
            AddStep<GenerateVirtualClassStep>();
            
            #endregion

            #region Class
            AddStep<FindClassCtorStep>();
            AddStep<GenerateClassFieldsStep>();

            AddStep<GenerateClassSpecialMethodDefsStep>();

            AddStep<GenerateClassGlobalPropStep>();
            AddStep<GenerateClassMethodDefStep>();
            AddStep<GenerateClassCtorStep>();

            AddStep<FindCastExplicitTypesStep>();
            AddStep<GenerateClassExplicitCastStep>();
            #endregion
            
            #region Arrow Function 
            AddStep<FindArrowFuncDefinitionStep>();
            AddStep<GenerateArrowFuncContextStep>();
            AddStep<FixArrowFuncContextNameStep>();

            if (Config.GeneratePseudocode)
            {
                AddStep<FindArrowFuncDefinitionStep>();
                AddStep<FindArrowFuncDefinitionStep>();
            }
            #endregion

            #region Hooks
            if (!Config.GeneratePseudocode)
            {
                AddStep<GenerateHooksClassStep>();
            }
            #endregion

            #region Modifiers
            AddStep<ApplyBuiltinModifierStep>();
            AddStep<SolveNameConflictStep>();
            #endregion
            
            AddStep<GenerateNativeImplClassStep>();
            AddStep<GenerateNativeFieldsDefStep>();

            #region Pseudocode
            if (Config.GeneratePseudocode)
            {
                AddStep<ScanGlobalValuesStep>();

                

                AddStep<FindMissingFunctionStep>();
                AddStep<CollectUnnamedFuncStep>();
                AddStep<GeneratePseudocodeStep>();
                AddStep<PostGeneratePseudocodeStep>();
            }
            else
            {
                if (Config.HaxeDocument != null)
                {
                    AddStep<GenerateXmlDocsStep>();
                }
            }
            #endregion
        }

        protected override void BeforeRun()
        {
            data.AddGlobalData(Config);
            data.AddGlobalData(new GlobalData(
                Config: Config,
                Assembly: output,
                Modifiers: [],
                Module: output.MainModule,
                Code: code,
                BytecodeMappingData: BytecodeMappingData,
                XmlDocument: XmlDocument
            ));
        }
    }
}
