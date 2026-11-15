using HashlinkNET.Compiler.Utils;
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
            if (type.FullName == "System.Void")
            {
                il.Emit(OpCodes.Call, ctx.RuntimeImports.phGetVoidTypePointer);
            }
            else if (type.FullName == "System.Object")
            {
                il.Emit(OpCodes.Call, ctx.RuntimeImports.phGetObjectTypePointer);
            }
            else
            {
                il.Emit(OpCodes.Call, ctx.RuntimeImports.phGetTypePointer.MakeInstance(type));
            }
            return ctx.RuntimeImports.bytesType;
        }
    }
}
