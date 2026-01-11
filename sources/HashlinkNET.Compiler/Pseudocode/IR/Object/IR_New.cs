using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Object
{
    class IR_New( 
        TypeReference type
        ) : IRBase
    {
        public TypeReference type = type;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            il.Emit(OpCodes.Call, 
                ctx.RuntimeImports.phCreateObject.MakeInstance(type));
            return type;
        }
    }
}
