using HashlinkNET.Compiler.Pseudocode.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
