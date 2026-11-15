using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    class IR_Throw(
        IRResult src
        ) : IRBase(src)
    {
        public readonly IRResult src = src;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var st = src.Emit(ctx, true);
            if (st != null && st.IsValueType && !st.IsGenericParameter)
            {
                il.Emit(OpCodes.Box, st);
            }
            il.Emit(OpCodes.Call, ctx.RuntimeImports.phWrapException);
            il.Emit(OpCodes.Throw);
            return null;
        }
    }
}
