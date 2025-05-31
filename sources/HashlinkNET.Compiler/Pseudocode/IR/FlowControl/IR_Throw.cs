using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    class IR_Throw(
        IRResult src
        ) : IRBase(src)
    {
        public readonly IRResult src = src;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src?.Emit(ctx, true);
            il.Emit(OpCodes.Throw);
            return null;
        }
    }
}
