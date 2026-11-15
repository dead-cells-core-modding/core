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

            // HashlinkArray indexer getter returns object.
            il.Emit(OpCodes.Callvirt, ctx.RuntimeImports.arrayGetItem);
            if (itemType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, itemType);
            }
            else if (itemType.FullName != "System.Object")
            {
                il.Emit(OpCodes.Castclass, itemType);
            }
            return itemType;
        }
    }
}
