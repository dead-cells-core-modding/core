using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Array
{
    class IR_GetArrayItem(
        IRResult src,
        IRResult index,
        TypeReference itemType
        ) : IRBase(src, index)
    {
        public readonly IRResult src = src;
        public readonly IRResult index = index;
        public TypeReference itemType = itemType;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            index.Emit(ctx, true);

            il.Emit(OpCodes.Ldelem_Any, itemType);
            return itemType;
        }
    }
}
