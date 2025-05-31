using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
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

            for (var i = 0; i < gdata.IRBasicBlocks.Count; i++)
            {
                var bb = gdata.IRBasicBlocks[i];
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
                    il);
                foreach (var v in bb.ir)
                {
                    v.Emit(ctx, false);
                }

                if (bb.defaultTransition != null)
                {
                    if (bb.defaultTransition.index != i + 1)
                    {
                        il.Emit(OpCodes.Br, bb.defaultTransition.startInst);
                    }
                }
                //il.Emit(OpCodes.Ldstr, "======BB End======");
                //il.Emit(OpCodes.Pop);
            }
            il.Emit(OpCodes.Ret);
            
        }
    }
}
