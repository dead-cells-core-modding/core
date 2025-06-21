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

namespace HashlinkNET.Compiler.Pseudocode.Steps
{
    class EmitILStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var rdata = container.GetGlobalData<RuntimeImports>();
            var md = gdata.Definition;


            md.Body.Instructions.Clear();
            var il = md.Body.GetILProcessor();
            var startInst = Instruction.Create(OpCodes.Nop);
            il.Append(startInst);
            var endInst = Instruction.Create(OpCodes.Nop);
            var mdsd = md.DebugInformation.Scope = new(startInst, endInst);
            var vds = new ScopeDebugInformation[gdata.Registers.Count];

            Queue<IRBasicBlockData> queue = [];
            BitArray visited = new(gdata.IRBasicBlocks.Count);

            queue.Enqueue(gdata.IRBasicBlocks[0]);

            while (queue.TryDequeue(out var bb))
            {
                if (visited[bb.index])
                {
                    continue;
                }
                visited[bb.index] = true;

                foreach (var v in bb.transitions)
                {
                    queue.Enqueue(v.Target);
                }

                il.Emit(OpCodes.Nop);

                //il.Emit(OpCodes.Ldstr, "======BB Start======");
                //il.Emit(OpCodes.Pop);

                il.Append(bb.startInst);
                var ctx = new EmitContext(
                    container,
                    md,
                    md.Module,
                    md.Module.TypeSystem,
                    rdata,
                    container.GetGlobalData<CompileConfig>(),
                    mdsd,
                    il)
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
                //il.Emit(OpCodes.Ldstr, "======BB End======");
                //il.Emit(OpCodes.Pop);
            }
            il.Emit(OpCodes.Ret);
            il.Append(endInst);
        }
    }
}
