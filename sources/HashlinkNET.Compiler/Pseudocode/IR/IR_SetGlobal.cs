using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_SetGlobal(
        IRResult val,
        int globalValue
        ) : IRBase(val)
    {
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            il.Emit(OpCodes.Ldc_I4, globalValue);
            val.Emit(ctx, true);
            il.Emit(OpCodes.Call, ctx.RuntimeImports.phSetGlobal);
            return null;
        }
    }
}
