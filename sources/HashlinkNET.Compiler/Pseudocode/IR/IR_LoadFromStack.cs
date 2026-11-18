using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Text;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    internal class IR_LoadFromStack(TypeReference? type) : IRBase
    {
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            return type;
        }
    }
}
