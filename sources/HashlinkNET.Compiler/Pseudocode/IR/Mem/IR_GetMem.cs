using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Mem
{
    internal class IR_GetMem(
        IRResult ptr,
        IRResult index,
        TypeReference type
        ) : IRBase(ptr, index)
    {
        public readonly IRResult ptr = ptr;
        public readonly IRResult index = index;
        public readonly TypeReference type = type;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container,
            ILProcessor il )
        {
            var pt = ptr.Emit(ctx, true)!;
            if (pt.IsByReference || pt.IsPointer)
            {
                il.Emit(OpCodes.Conv_U);
            }

            index.Emit(ctx, true);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Add);

            il.Emit(OpCodes.Call, ctx.RuntimeImports.unsafeRead.MakeInstance(type));
            return type;
        }
    }
}
