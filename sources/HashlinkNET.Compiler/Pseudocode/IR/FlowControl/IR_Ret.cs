using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    class IR_Ret(
        IRResult retValue
        ) : IRBase(retValue)
    {
        public IRResult retValue = retValue;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {

            retValue.Emit(ctx, !retValue.IsNone);
            il.Emit(OpCodes.Ret);
            return null;
        }
    }
}
