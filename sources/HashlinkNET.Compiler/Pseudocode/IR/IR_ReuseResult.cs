using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Text;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_ReuseResult( IRResult result ) : IRBase(result)
    {
        private VariableDefinition? cachedVariable;
        private Instruction? stlocInst;
        private readonly bool reusable = !result.HasSideEffects;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (reusable)
            {
                return result.Emit(ctx, true);
            }

            if (cachedVariable == null)
            {
                var t = result.Emit(ctx, true);
                stlocInst = Instruction.Create(OpCodes.Nop);

                cachedVariable = new(t);
                il.Append(stlocInst);
                return cachedVariable.VariableType;
            }
            if (stlocInst != null)
            {
                stlocInst.OpCode = OpCodes.Dup;
                stlocInst.Operand = cachedVariable;
                ctx.Definition.Body.Variables.Add(cachedVariable);

                il.InsertBefore(stlocInst, Instruction.Create(OpCodes.Dup));

                stlocInst = null;
            }
            il.Emit(OpCodes.Ldloc, cachedVariable);
            return cachedVariable.VariableType;
        }
    }
}
