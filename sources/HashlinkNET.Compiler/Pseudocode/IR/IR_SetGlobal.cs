using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_SetGlobal(
        IRResult val,
        int globalValue
        ) : IRBaseWithSideEffects(val)
    {
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            il.Emit(OpCodes.Ldc_I4, globalValue);
            var st = val.Emit(ctx, true);
            if (st != null && st.IsValueType && !st.IsGenericParameter)
            {
                il.Emit(OpCodes.Box, st);
            }
            il.Emit(OpCodes.Call, ctx.RuntimeImports.phSetGlobal);
            il.Emit(OpCodes.Pop);
            return null;
        }
    }
}
