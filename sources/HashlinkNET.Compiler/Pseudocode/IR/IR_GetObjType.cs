using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_GetObjType(
        IRResult src
        ) : IRBase(src)
    {
        public readonly IRResult src = src;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var st = src.Emit(ctx, true);
            if (st is not null && st.IsValueType && !st.IsGenericParameter)
            {
                il.Emit(OpCodes.Box, st);
            }
            il.Emit(OpCodes.Call, ctx.RuntimeImports.phGetTypeId);
            return ctx.RuntimeImports.typeSystem.Int32;
        }
    }
}
