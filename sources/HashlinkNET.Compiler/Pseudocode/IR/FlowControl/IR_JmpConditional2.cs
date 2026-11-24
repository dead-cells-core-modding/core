using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Utils;
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
            var a_r = new IR_ReuseResult(a);
            var b_r = new IR_ReuseResult(b);

            var isEquality = kind is ConditionKind.Eq or ConditionKind.NotEq;

            var at = a_r.Emit(ctx, true);
            var aIsByRef = at is ByReferenceType;
            var aIsValue = at is { IsValueType: true };
            if (isEquality && aIsValue)
            {
                il.Emit(OpCodes.Box, at!);
            }
            else if (at != null && at.IsPointer)
            {
                il.Emit(OpCodes.Conv_I);
            }

            var bt = b_r.Emit(ctx, true);
            var bIsByRef = bt is ByReferenceType;
            var bIsValue = bt is { IsValueType: true };
            if (isEquality && bIsValue)
            {
                il.Emit(OpCodes.Box, bt!);
            }
            else if (bt != null && bt.IsPointer)
            {
                il.Emit(OpCodes.Conv_I);
            }

            if (container.TryGetData<IObjComparable>(at, out var compare) && compare.Compare is not null)
            {
                var finalLabel = Instruction.Create(OpCodes.Nop);

                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Dup);

                il.Emit(OpCodes.Brfalse, finalLabel);

                il.Emit(OpCodes.Pop);

                a_r.Emit(ctx, true);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue, finalLabel);

                il.Emit(OpCodes.Pop);

                a_r.Emit(ctx, true);
                b_r.Emit(ctx, true);

                il.Emit(OpCodes.Call, compare.Compare);

                il.Append(finalLabel);
                il.Emit(OpCodes.Ldc_I4_0);
            }
            else if (isEquality && aIsByRef && bIsByRef)
            {
                // Managed pointers cannot be compared with beq/bne directly.
                il.Emit(OpCodes.Call, ctx.RuntimeImports.unsafeAreSame.MakeInstance(
                    ((ByReferenceType)at!).ElementType));
                il.EmitProtectedBranch(kind == ConditionKind.Eq ? OpCodes.Brtrue : OpCodes.Brfalse,
                    target.startInst);
                return null;
            }
            else if (isEquality)
            {
                // Non-primitive value types (e.g. Nullable<T>) cannot use
                // beq/bne; compare the boxed values instead.
                il.Emit(OpCodes.Call, ctx.RuntimeImports.phCompare);
                il.EmitProtectedBranch(kind == ConditionKind.Eq ? OpCodes.Brtrue : OpCodes.Brfalse,
                    target.startInst);
                return null;
            }

            

            il.EmitProtectedBranch(kind switch
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
