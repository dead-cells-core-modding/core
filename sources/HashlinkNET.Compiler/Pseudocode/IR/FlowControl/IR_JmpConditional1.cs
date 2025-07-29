using HashlinkNET.Compiler.Pseudocode.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    class IR_JmpConditional1(
        IRBasicBlockData target,
        IR_JmpConditional1.ConditionKind kind,
        IRResult src
        ) : IRBase(src), IIR_JmpConditional
    {
        public void ReserveCondition()
        {
            kind = kind switch
            {
                ConditionKind.True => ConditionKind.False,
                ConditionKind.False => ConditionKind.True,
                ConditionKind.Null => ConditionKind.NotNull,
                ConditionKind.NotNull => ConditionKind.Null,
                _ => throw new NotImplementedException()
            };
        }
        public IRBasicBlockData Target
        {
            get => target;
            set => target = value;
        }
        public enum ConditionKind
        {
            True,
            False,
            Null,
            NotNull
        }
        public IRBasicBlockData target = target;
        public ConditionKind kind = kind;
        public readonly IRResult src = src;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);

            il.Emit(kind switch
            {
               ConditionKind.True => OpCodes.Brtrue,
               ConditionKind.False => OpCodes.Brfalse,
               ConditionKind.Null => OpCodes.Brfalse,
               ConditionKind.NotNull => OpCodes.Brtrue,
                _ => throw new NotSupportedException()
            }, target.startInst);

            return null;
        }
    }
}
