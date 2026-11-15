using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Ref
{
    class IR_RefArray(
        IRResult src,
        IRResult index,
        TypeReference type
        ) : IRBase(src, index)
    {
        public readonly IRResult src = src;
        public readonly IRResult index = index;
        public TypeReference refType = type;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var et = refType is ByReferenceType byRef
                ? byRef.ElementType
                : refType is GenericInstanceType git && git.GenericArguments.Count == 1
                    ? git.GenericArguments[0]
                    : ctx.TypeSystem.Byte;

            var st = src.Emit(ctx, true)!;
            if (st.IsByReference || st.IsPointer)
            {
                il.Emit(OpCodes.Conv_U);
            }

            index.Emit(ctx, true);
            il.Emit(OpCodes.Sizeof, et);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Add);

            il.Emit(OpCodes.Call, ctx.RuntimeImports.unsafeAsRef.MakeInstance(et));
            return et.MakeByReferenceType();
        }
    }
}
