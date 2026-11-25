using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    class IR_Rethrow : IRBase
    {
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            il.Emit(OpCodes.Rethrow);
            return null;
        }
    }
}
