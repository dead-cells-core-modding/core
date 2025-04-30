using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Steps.Preprocessor.Imports;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Fun
{
    internal class GenerateFuncBaseTypeStep : CompileStep
    {
        private TypeDefinition GenerateDelegate( ModuleDefinition module,
            RuntimeImports rdata,
            bool hasRet, int argCount )
        {
            var ts = module.TypeSystem;
            var type = new TypeDefinition("HaxeProxy.Runtime", !hasRet ?
                "HlAction`" + argCount : "HlFunc`" + (argCount + 1), TypeAttributes.Class | TypeAttributes.Public 
                | TypeAttributes.Sealed)
            {
                BaseType = rdata.delegateBaseType
            };
            module.Types.Add(type);

            TypeReference retType;
            if (!hasRet)
            {
                retType = module.TypeSystem.Void;
            }
            else
            {
                var pd = new GenericParameter("TRet", type)
                {
                    AllowByRefLikeConstraint = true
                };
                type.GenericParameters.Add(pd);
                retType = pd;
            }

            var argTypes = new TypeReference[argCount];
            for (var i = 0; i < argCount; i++)
            {
                var pd = new GenericParameter("TArg" + (i + 1), type)
                {
                    AllowByRefLikeConstraint = true
                };
                type.GenericParameters.Add(pd);
                argTypes[i] = pd;
            }


            var paramsArray = argTypes.Select(x => new ParameterDefinition(x)).ToArray();

            type.Methods.Add(new MethodDefinition(".ctor", MethodAttributes.Public, ts.Void)
            {
                Parameters =
                {
                    new(ts.Object),
                    new(ts.IntPtr)
                },
                HasThis = true,
                IsVirtual = true,
                IsRuntimeSpecialName = true,
                IsSpecialName = true,
                IsHideBySig = true,
                IsRuntime = true,
                Body = null
            });
            type.Methods.Add(new MethodDefinition("EndInvoke", MethodAttributes.Public, retType)
            {
                Parameters =
                {
                    new(rdata.IAsyncResultType),
                },
                HasThis = true,
                IsVirtual = true,
                IsHideBySig = true,
                IsRuntime = true,
                IsNewSlot = true,
                Body = null
            });
            var invoke = new MethodDefinition("Invoke", MethodAttributes.Public, retType)
            {
                HasThis = true,
                IsVirtual = true,
                IsHideBySig = true,
                IsRuntime = true,
                IsNewSlot = true,
                Body = null
            };
            type.Methods.Add(invoke);
            var beginInvoke = new MethodDefinition("BeginInvoke", MethodAttributes.Public, rdata.IAsyncResultType)
            {
                HasThis = true,
                IsVirtual = true,
                IsHideBySig = true,
                IsRuntime = true,
                IsNewSlot = true,
                Body = null
            };
            type.Methods.Add(beginInvoke);
            foreach (var v in argTypes)
            {
                invoke.Parameters.Add(new(v));
                beginInvoke.Parameters.Add(new(v));
            }
            beginInvoke.Parameters.Add(new(rdata.AsyncCallbackType));
            beginInvoke.Parameters.Add(new(ts.Object));


            return type;
        }
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<GlobalData>();
            var rdata = container.GetGlobalData<RuntimeImports>();
            var maxArgCount = 0;
            foreach (var v in gdata.Code.Types.OfType<HlTypeWithFun>())
            {
                var argCount = v.FunctionDescription.Arguments.Length + 1;
                if (argCount > maxArgCount)
                {
                    maxArgCount = argCount;
                }
            }
            var fts = container.AddGlobalData<FunctionTypes>(new(
            
                ActionTypes: new TypeReference[maxArgCount],
                FuncTypes: new TypeReference[maxArgCount]
            ));
            for (var i = 0; i < ImportRuntimeTypesStep.FUNC_MAX_ARGS_COUNT; i++)
            {
                if (i >= maxArgCount)
                {
                    return;
                }
                fts.ActionTypes[i] = rdata.actionTypes[i];
                fts.FuncTypes[i] = rdata.funcTypes[i];
            }

            //Generate New Delegate
            for (var i = ImportRuntimeTypesStep.FUNC_MAX_ARGS_COUNT; i < maxArgCount; i++)
            {
                fts.ActionTypes[i] = GenerateDelegate(gdata.Module, rdata, false, i);
                fts.FuncTypes[i] = GenerateDelegate(gdata.Module, rdata, true, i);
            }
        }
    }
}
