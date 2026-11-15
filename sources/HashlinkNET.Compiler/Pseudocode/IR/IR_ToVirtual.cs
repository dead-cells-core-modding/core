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
            var st = src.Emit(ctx, true);
            if (st != null &&
                !st.IsValueType &&
                !st.IsByReference &&
                st.FullName != ctx.RuntimeImports.objBaseType.FullName)
            {
                il.Emit(OpCodes.Castclass, ctx.RuntimeImports.objBaseType);
            }
            il.Emit(OpCodes.Callvirt, ctx.RuntimeImports.hToVirtual.MakeInstance(type));
            return type;
        }
    }
}
