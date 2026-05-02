using BytecodeMapping;
using HashlinkNET.Bytecode;
using HashlinkNET.Compiler;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.Steps;
using HashlinkNET.Compiler.Pseudocode.Steps.Backend;
using HashlinkNET.Compiler.Steps;
using HashlinkNET.Compiler.Steps.Preprocessor.Imports;
using Haxe2CSharp.Steps;
using HaxeProxy.Runtime;
using HaxeProxy.Runtime.Internals;
using Mono.Cecil;
using System;
using System.Diagnostics;

namespace Haxe2CSharp
{
    internal class FuncCompiler2(CompilerContext ctx) : BaseCompiler
    {
        protected override void BeforeRun()
        {
            ((DataContainer)data).TryResolve += FuncCompiler2_TryResolve;


            var cf = new CompileConfig();

            //data.Parent = parent;
            data.AddGlobalData<FuncEmitGlobalData>(new(
                ctx.FuncCode,
                ((HlTypeWithFun)ctx.FuncCode.Type.Value).FunctionDescription,
                ctx.Method,
                null!
                ));

            data.AddGlobalData(cf);
            data.AddGlobalData(ctx);
            data.AddGlobalData(new GlobalData(
                cf, ctx.Module.Assembly, ctx.Module, [], ctx.Code, new(), new()
                ));

        }

        private object? FuncCompiler2_TryResolve( object arg1, Type arg2 )
        {
            if (arg2 == typeof(TypeReference))
            {
                if (arg1 is HlType hltype)
                {
                    var bt = HaxeProxyManager.bindingTypes[hltype.TypeIndex];

                    Debug.Assert(bt != null);
                    return ctx.Module.ImportReference(
                       bt
                        );
                }
            }
            if (arg2 == typeof(MethodDefinition))
            {
            
            }

            return null;
        }

        protected override void InstallSteps()
        {
            AddStep<ImportRuntimeTypesStep>();
            AddStep<GenerateFuncRegsStep>();
            AddStep<SplitBasicBlocksStep>();

            AddStep<ParseOpCodesStep2>();

            AddStep<LinearizeBasicBlocksStep>();
            AddStep<EmitILStep>();
        }
    }
}
