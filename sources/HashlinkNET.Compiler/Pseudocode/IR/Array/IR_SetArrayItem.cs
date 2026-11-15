using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Array
{
    class IR_SetArrayItem(
        IRResult src,
        IRResult index,
        IRResult val
        ) : IRBase(src, index, val)
    {
        public readonly IRResult src = src;
        public readonly IRResult index = index;
        public readonly IRResult val = val;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            index.Emit(ctx, true);
            var rt = val.Emit(ctx, true)!;
            if (rt.IsValueType && !rt.IsGenericParameter)
            {
                il.Emit(OpCodes.Box, rt);
            }
            il.Emit(OpCodes.Callvirt, ctx.RuntimeImports.arraySetItem);
            return null;
        }
    }
}
