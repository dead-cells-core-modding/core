using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.EnumOpt
{
    internal class IR_EnumIndex(
        IRBase input
        ) : IRBase(input)
    {
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, 
            ILProcessor il )
        {
            throw new NotImplementedException();
        }
    }
}
