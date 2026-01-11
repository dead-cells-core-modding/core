using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_GetType(
        TypeReference type
        ) : IRBase
    {
        public TypeReference type = type;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            il.Emit(OpCodes.Ldtoken, type);
            il.Emit(OpCodes.Call, ctx.RuntimeImports.typeGetFromHandleMethod);
            return ctx.RuntimeImports.typeType;
        }
    }
}
