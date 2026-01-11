using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    class IR_Assert(
        string msg
        ) : IRBase
    {
        public string msg = msg;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            il.Emit(OpCodes.Break);
            il.Emit(OpCodes.Ldstr, msg);
            il.Emit(OpCodes.Throw);
            return null;
        }
    }
}
