using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Ref
{
    class IR_Unref(
        IRResult src,
        TypeReference type
        ) : IRBase(src)
    {
        public readonly IRResult src = src;
        public readonly TypeReference itemType = type;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            il.Emit(OpCodes.Ldobj, itemType);
            return itemType;
        }
    }
}
