using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Mem
{
    internal class IR_SetMem(
        IRResult ptr,
        IRResult index,
        IRResult val,
        TypeReference? storeType = null
        ) : IRBaseWithSideEffects(ptr, index, val)
    {
        public readonly IRResult ptr = ptr;
        public readonly IRResult val = val;
        public readonly IRResult index = index;
        public TypeReference? storeType = storeType;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var pt = ptr.Emit(ctx, true)!;
            if (pt.IsByReference || pt.IsPointer)
            {
                il.Emit(OpCodes.Conv_U);
            }

            index.Emit(ctx, true);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Add);

            var vt = val.Emit(ctx, true)!;
            vt = il.EmitNullableUnwrap(container, vt)!;
            var target = storeType ?? vt;
            if (storeType != null)
            {
                switch (storeType.Name)
                {
                    case nameof(Byte):
                        il.Emit(OpCodes.Conv_U1);
                        break;
                    case nameof(SByte):
                        il.Emit(OpCodes.Conv_I1);
                        break;
                    case nameof(Int16):
                        il.Emit(OpCodes.Conv_I2);
                        break;
                    case nameof(UInt16):
                        il.Emit(OpCodes.Conv_U2);
                        break;
                }
            }

            il.Emit(OpCodes.Call, ctx.RuntimeImports.unsafeWrite.MakeInstance(target));
            return null;
        }
    }
}
