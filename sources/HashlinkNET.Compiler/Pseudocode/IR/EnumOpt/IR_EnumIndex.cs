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

            il.Emit(OpCodes.Callvirt, ctx.RuntimeImports.hGetEnumIndex);

            // The raw enum index is an int32. The generated Indexes type is a
            // CLR enum (a value type), so castclass would be invalid IL.
            if (tr is not null &&
                container.TryGetData<EnumClassData>(tr, out var ecd))
            {
                return ecd.IndexType;
            }
            return ctx.TypeSystem.Int32;
        }
    }
}
