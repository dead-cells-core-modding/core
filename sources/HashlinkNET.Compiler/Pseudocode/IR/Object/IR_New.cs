using HashlinkNET.Compiler.Pseudocode.IR;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
