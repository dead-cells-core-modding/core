using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Array
{
    class IR_GetArraySize(
        IRResult src
        ) : IRBase(src)
    {
        public readonly IRResult src = src;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            il.Emit(OpCodes.Ldlen);
            return ctx.TypeSystem.Int32;
        }
    }
}
