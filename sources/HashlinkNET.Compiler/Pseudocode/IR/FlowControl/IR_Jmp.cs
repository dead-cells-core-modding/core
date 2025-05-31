using HashlinkNET.Compiler.Pseudocode.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    class IR_Jmp(
        IRBasicBlockData target
        ) : IRBase
    {
        public IRBasicBlockData target = target;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            il.Emit(OpCodes.Br, target.startInst);
            return null;
        }
    }
}
