using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Pseudocode.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    class IR_JmpConditional2(
        IRBasicBlockData target,
        IR_JmpConditional2.ConditionKind kind,
        IRResult a,
        IRResult b,
        bool useEqualityOperator = false
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
                ConditionKind.UGreate => ConditionKind.UNotGreate,
                ConditionKind.UNotGreate => ConditionKind.UGreate,
                _ => throw new NotImplementedException()
            };
        }
        public IRBasicBlockData Target {
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
            NotEq,
            UGreate,
            UNotGreate,
        }
        public IRBasicBlockData target = target;
        public ConditionKind kind = kind;
        public readonly IRResult a = a;
        public readonly IRResult b = b;
        public readonly bool useEqualityOperator = useEqualityOperator;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var at = a.Emit(ctx, true);
            b.Emit(ctx, true);

            if (useEqualityOperator && kind is ConditionKind.Eq or ConditionKind.NotEq)
            {
                // Haxe proxy types may overload == / != (for example HaxeEnum
                // compares structurally), so use the operator instead of a
                // raw reference comparison.
                il.Emit(OpCodes.Call, kind == ConditionKind.Eq
                    ? ctx.RuntimeImports.hEnumEquality
                    : ctx.RuntimeImports.hEnumInequality);
                il.Emit(OpCodes.Brtrue, target.startInst);
                return null;
            }

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
                ConditionKind.UGreate => OpCodes.Bge_Un,
                ConditionKind.UNotGreate => OpCodes.Blt_Un,
                _ => throw new NotSupportedException()
            }, target.startInst);

            return null;
        }
    }
}
