using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    class EmitILStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var rdata = container.GetGlobalData<RuntimeImports>();
            var md = gdata.Definition;

            var list = container.GetGlobalData<List<IRBasicBlockData>>();


            md.Body.Instructions.Clear();
            var il = md.Body.GetILProcessor();
            var startInst = Instruction.Create(OpCodes.Nop);
            il.Append(startInst);
            var endInst = Instruction.Create(OpCodes.Nop);
            var mdsd = md.DebugInformation.Scope = new(startInst, endInst);
            var vds = new ScopeDebugInformation[gdata.Registers.Count];

            

            foreach (var bb in list)
            {
                il.Emit(OpCodes.Nop);

                il.Append(bb.startInst);
                var ctx = new EmitContext(
                    container,
                    md,
                    md.Module,
                    md.Module.TypeSystem,
                    rdata,
                    container.GetGlobalData<CompileConfig>(),
                    mdsd,
                    il,
                    gdata)
                {
                    VariableDebugs = vds
                };

                foreach (var v in bb.ir)
                {
                    v.Emit(ctx, false);
                }

                if (bb.defaultTransition != null)
                {
                    il.Emit(OpCodes.Br, bb.defaultTransition.startInst);
                }
                il.Append(bb.endInst);
            }

            il.Emit(OpCodes.Ret);
            il.Append(endInst);
        }
    }
}
