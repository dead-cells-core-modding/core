using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    /// <summary>
    /// Evaluates an expression for its side effects and discards any value it
    /// leaves on the evaluation stack.
    /// </summary>
    internal class IR_Discard(
        IRResult src
        ) : IRBase(src)
    {
        public readonly IRResult src = src;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container,
            ILProcessor il )
        {
            // requestValue=false makes IRBase pop any produced value.
            src.Emit(ctx, false);
            return null;
        }
    }
}
