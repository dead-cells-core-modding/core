using HashlinkNET.Bytecode;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Call
{
    internal class IR_GetNative(
        HlNative native
        ) : IRBase()
    {
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            /*il.Emit(OpCodes.Ldstr, native.Lib);
            il.Emit(OpCodes.Ldstr, native.Name);
            il.Emit(OpCodes.Call, ctx.RuntimeImports.phGetNativeMethod);
            return ctx.RuntimeImports.delegateBaseType;*/
            var f = container.GetData<FieldReference>(native);
            il.Emit(OpCodes.Ldsfld, f);
            return f.FieldType;
        }
    }
}
