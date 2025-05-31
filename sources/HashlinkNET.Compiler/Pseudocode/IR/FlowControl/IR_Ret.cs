using HashlinkNET.Compiler.Pseudocode.IR;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
