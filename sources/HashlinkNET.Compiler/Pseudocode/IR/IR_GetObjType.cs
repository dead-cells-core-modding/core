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
            src.Emit(ctx, true);
            il.Emit(OpCodes.Callvirt, ctx.RuntimeImports.objectGetTypeMethod);
            return ctx.RuntimeImports.typeType;
        }
    }
}
