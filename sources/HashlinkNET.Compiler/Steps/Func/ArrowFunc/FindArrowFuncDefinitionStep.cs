using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func.ArrowFunc
{
    internal class FindArrowFuncDefinitionStep : ParallelCompileStep<HlFunction>
    {
        protected override void Execute( IDataContainer container, HlFunction item, int index )
        {
            var type = (HlTypeWithFun)item.Type.Value;
            var desc = type.FunctionDescription;
            if (desc.Arguments.Length == 0)
            {
                return;
            }
            var firstArg = desc.Arguments[0].Value;
            var md = container.GetData<FuncData>(item);
            if (!container.TryGetData<ArrowFuncContextData>(firstArg, out var ctx))
            {
                if (md.DeclaringClass != null)
                {
                    return;
                }
                //No Context Arrow Function
                TypeDefinition? parentDef = null;
                MethodDefinition? parentM = null;
                foreach ((var parent, _) in md.UsedBy)
                {
                    if (parent.DeclaringClass != null)
                    {
                        parentM = parent.Definition;
                        parentDef = parent.DeclaringClass.TypeDef;
                        goto BREAK_0;
                    }
                }
                BREAK_0:
                if (parentDef == null)
                {
                    return;
                }
                md.Definition.Name = "ArrowFunction_" + parentM!.Name + "_" + item.FunctionIndex;
                if (md.Definition.Parameters.Count > 0)
                {
                    var fp = md.Definition.Parameters[0];
                    if (fp.ParameterType == parentDef)
                    {
                        md.Definition.Parameters.RemoveAt(0);
                        md.Definition.HasThis = true;
                        md.Definition.IsStatic = false;
                    }
                }
                RunSync(() => parentDef.Methods.Add(md.Definition));
                return;
            }
            
            ctx.Methods.Add(md);
            RunSync(() => ctx.TypeDef.Methods.Add(md.Definition));
        }

        protected override IReadOnlyList<HlFunction> GetItems( IDataContainer container )
        {
            return container.GetGlobalData<GlobalData>().Code.Functions;
        }
    }
}
