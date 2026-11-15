using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
        public IRBasicBlockData Target {
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
            var st = src.Emit(ctx, true);
            if (st is ByReferenceType byRef)
            {
                // brtrue/brfalse do not accept managed pointers, so test the
                // reference with Unsafe.IsNullRef<T>.
                il.Emit(OpCodes.Call,
                    ctx.RuntimeImports.unsafeIsNullRef.MakeInstance(byRef.ElementType));
                il.EmitProtectedBranch(kind switch
                {
                    ConditionKind.True => OpCodes.Brfalse,
                    ConditionKind.False => OpCodes.Brtrue,
                    ConditionKind.Null => OpCodes.Brtrue,
                    ConditionKind.NotNull => OpCodes.Brfalse,
                    _ => throw new NotSupportedException()
                }, target.startInst);
                return null;
            }
            if (st != null && st.IsValueType && !st.IsPrimitive)
            {
                // brtrue/brfalse need a primitive/object; box non-primitive
                // value types (e.g. Nullable<T>).
                il.Emit(OpCodes.Box, st);
            }
            else if (st != null && st.IsPointer)
            {
                il.Emit(OpCodes.Conv_I);
            }

            il.EmitProtectedBranch(kind switch
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
