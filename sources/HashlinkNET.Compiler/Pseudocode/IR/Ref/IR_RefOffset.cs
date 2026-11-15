using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

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
            var st = src.Emit(ctx, true)!;
            var et = st is ByReferenceType byRef
                ? byRef.ElementType
                : ctx.TypeSystem.Byte;

            if (st.IsByReference || st.IsPointer)
            {
                il.Emit(OpCodes.Conv_U);
            }

            offset.Emit(ctx, true);
            il.Emit(OpCodes.Sizeof, et);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Add);

            il.Emit(OpCodes.Call, ctx.RuntimeImports.unsafeAsRef.MakeInstance(et));
            return et.MakeByReferenceType();
        }
    }
}
