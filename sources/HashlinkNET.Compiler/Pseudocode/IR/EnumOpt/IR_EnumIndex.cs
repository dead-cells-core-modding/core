using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.EnumOpt
{
    internal class IR_EnumIndex(
        IRBase input
        ) : IRBase(input)
    {
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, 
            ILProcessor il )
        {
            var tr = input.Emit(ctx, true);

            il.Emit(OpCodes.Call, ctx.RuntimeImports.hGetEnumIndex);

            if (tr is not null)
            {
                if (container.TryGetData<EnumClassData>(tr, out var ecd))
                {
                    il.Emit(OpCodes.Castclass, ecd.IndexType);
                }
            }
            return ctx.TypeSystem.Int32;
        }
    }
}
