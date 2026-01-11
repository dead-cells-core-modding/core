using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Ref
{
    class IR_SetRef(
        IRResult ptr,
        IRResult val
        ) : IRBase(ptr, val)
    {
        public readonly IRResult ptr = ptr;
        public readonly IRResult val = val;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            ptr.Emit(ctx, true);
            var vt = val.Emit(ctx, true);
            il.Emit(OpCodes.Stobj, vt);
            return null;
        }
    }
}
