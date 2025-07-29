using HashlinkNET.Compiler.Data.Interfaces;
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
    class IR_JmpConditional2(
        IRBasicBlockData target,
        IR_JmpConditional2.ConditionKind kind,
        IRResult a,
        IRResult b
        ) : IRBase(a, b), IIR_JmpConditional
    {
        public void ReserveCondition()
        {
            kind = kind switch
            {
                ConditionKind.Eq => ConditionKind.NotEq,
                ConditionKind.NotEq => ConditionKind.Eq,
                ConditionKind.Greate => ConditionKind.NotGreate,
                ConditionKind.Less => ConditionKind.NotLess,
                ConditionKind.NotGreate => ConditionKind.Greate,
                ConditionKind.NotLess => ConditionKind.Less,
                ConditionKind.SGreate => ConditionKind.NotGreate,
                ConditionKind.SLess => ConditionKind.NotLess,
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
            Eq,
            Greate,
            Less,
            SGreate,
            SLess,
            NotGreate,
            NotLess,
            NotEq
        }
        public IRBasicBlockData target = target;
        public ConditionKind kind = kind;
        public readonly IRResult a = a;
        public readonly IRResult b = b;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var at = a.Emit(ctx, true);
            b.Emit(ctx, true);

            if (container.TryGetData<IObjComparable>(at, out var compare) && compare.Compare is not null)
            {
                il.Emit(OpCodes.Call, compare.Compare);
                il.Emit(OpCodes.Ldc_I4_0);
            }

            il.Emit(kind switch
            {
                ConditionKind.Eq => OpCodes.Beq,
                ConditionKind.NotEq => OpCodes.Bne_Un,
                ConditionKind.Greate => OpCodes.Bgt,
                ConditionKind.NotGreate => OpCodes.Ble,
                ConditionKind.Less => OpCodes.Blt,
                ConditionKind.NotLess => OpCodes.Bge,
                ConditionKind.SGreate => OpCodes.Bgt_Un,
                ConditionKind.SLess => OpCodes.Blt_Un,
                _ => throw new NotSupportedException()
            }, target.startInst);

            return null;
        }
    }
}
