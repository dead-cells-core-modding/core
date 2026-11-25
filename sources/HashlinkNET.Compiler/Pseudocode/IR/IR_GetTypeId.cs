using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_GetTypeId(
        IRResult src
        ) : IRBase(src)
    {
        public readonly IRResult src = src;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var st = src.Emit(ctx, true);
            Debug.Assert(st is not null && st.IsValueType);

            il.Emit(OpCodes.Call, ctx.RuntimeImports.phGetTypeId);
            return ctx.RuntimeImports.typeSystem.Int32;
        }
    }
}
