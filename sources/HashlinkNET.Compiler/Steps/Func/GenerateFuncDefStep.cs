using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func
{
    internal class GenerateFuncDefStep : ParallelCompileStep<HlFunction>
    {
        private GlobalData gdata = null!;
        private RuntimeImports rdata = null!;

        protected override void Initialize( IDataContainer container )
        {
            base.Initialize(container);

            gdata = container.GetGlobalData<GlobalData>();
            rdata = container.GetGlobalData<RuntimeImports>();

        }
        protected override void Execute( IDataContainer container, HlFunction f, int index )
        {

            var ft = ((HlTypeWithFun)f.Type.Value).FunctionDescription;
            var md = new MethodDefinition("Func" + f.FunctionIndex, MethodAttributes.Public | MethodAttributes.HideBySig, gdata.Module.TypeSystem.Void)
            {
                HasThis = false,
                IsStatic = true,
                CustomAttributes =
                    {
                        new(rdata.attrFIndexCtor)
                        {
                            ConstructorArguments =
                            {
                                new(gdata.Module.TypeSystem.Int32,f.FunctionIndex)
                            }
                        }
                    },
                ReturnType = container.GetTypeRef(ft.ReturnType.Value),
                CallingConvention = MethodCallingConvention.Default,
            };
            for (int i = 0; i < ft.Arguments.Length; i++)
            {
                var at = container.GetTypeRef(ft.Arguments[i].Value);
                md.Parameters.Add(new("arg" + (i + 1), ParameterAttributes.None, at));
            }

            if (f.Assigns != null && md.Parameters.Count > 0)
            {
                var argNamesCount = f.Assigns.TakeWhile(x => x.Index == 0).Count();
                var ai = 0;
                for (int i = md.Parameters.Count != argNamesCount ? 1 : 0; i < md.Parameters.Count; i++)
                {
                    if (ai >= f.Assigns.Length)
                    {
                        break;
                    }
                    md.Parameters[i].Name = f.Assigns[ai++].Name;
                }
            }

            md.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            md.Body.Instructions.Add(Instruction.Create(OpCodes.Throw));

            container.AddData(f, md);
        }

        protected override IReadOnlyList<HlFunction> GetItems( IDataContainer container )
        {
            return container.GetGlobalData<GlobalData>().Code.Functions;
        }
    }
}
