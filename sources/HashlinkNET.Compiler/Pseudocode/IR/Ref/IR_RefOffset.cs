using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Ref
{
    class IR_RefOffset(
        IRResult src,
        IRResult offset
        ) : IRBase(src, offset)
    {
        public readonly IRResult src = src;
        public readonly IRResult offset = offset;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var rt = src.Emit(ctx, true);
            offset.Emit(ctx, true);
            il.Emit(OpCodes.Sizeof, ((ByReferenceType)rt!).ElementType);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Add);
            return rt;
        }
    }
}
