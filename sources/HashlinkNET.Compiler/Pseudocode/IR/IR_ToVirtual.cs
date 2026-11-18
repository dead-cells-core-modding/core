using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_ToVirtual(
        IRResult src,
        TypeReference type
        ) : IRBase(src)
    {
        public readonly IRResult src = src;
        public TypeReference type = type;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);

            il.Emit(OpCodes.Call, ctx.RuntimeImports.phCast.MakeInstance(type));
            return type;
        }
    }
}
