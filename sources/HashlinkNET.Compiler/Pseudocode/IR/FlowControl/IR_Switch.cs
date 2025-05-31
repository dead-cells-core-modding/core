using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.IR;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    class IR_Switch(
        IRResult src,
        params IRBasicBlockData[] targets
        ) : IRBase(src)
    {
        public readonly IRResult src = src;
        public IRBasicBlockData[] targets = targets;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            var insts = new Instruction[targets.Length];
            for (var i = 0; i < targets.Length; i++)
            {
                insts[i] = targets[i].startInst;
            }
            il.Emit(OpCodes.Switch, insts);
            return null;
        }
    }
}
