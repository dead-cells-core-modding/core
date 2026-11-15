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
            else
            {
                il.Emit(OpCodes.Ldtoken, type);
                il.Emit(OpCodes.Call, ctx.RuntimeImports.typeGetTypeFromHandle);
                il.Emit(OpCodes.Call, ctx.RuntimeImports.phGetTypePointerByType);
            }
            return ctx.RuntimeImports.bytesType;
        }
    }
}
