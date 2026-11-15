using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.DynObj
{
    class IR_SetDynObj(
        IRResult src,
        IRResult val,
        string name
        ) : IRBase(src, val)
    {
        public readonly IRResult src = src;
        public readonly IRResult val = val;
        public string name = name;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            il.Emit(OpCodes.Ldstr, name);
            var vt = val.Emit(ctx, true);
            if (vt != null && vt.IsValueType && !vt.IsGenericParameter)
            {
                il.Emit(OpCodes.Box, vt);
            }
            il.Emit(OpCodes.Call, ctx.RuntimeImports.phDynSetMethod);
            return null;
        }
    }
}
