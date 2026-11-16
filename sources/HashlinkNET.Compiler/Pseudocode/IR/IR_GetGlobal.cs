using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_GetGlobal(
        IGlobalValue globalValue
        ) : IRBase
    {
        public IGlobalValue globalValue = globalValue;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (globalValue.GlobalClassProp is not null)
            {
                il.Emit(OpCodes.Call, globalValue.GlobalClassProp.GetMethod);
                return globalValue.GlobalClassType;
            }
            else
            {
                // No managed proxy exists for this global value.
                HashlinkMethodMarker.MarkShouldUseNative(ctx.DataContainer, ctx.Definition);
                il.Emit(OpCodes.Ldnull);
                return ctx.TypeSystem.Object;
            }
        }
    }
}

