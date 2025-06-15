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
            input.Emit(ctx, true);
            il.Emit(OpCodes.Call, ctx.RuntimeImports.hGetEnumIndex);
            return ctx.TypeSystem.Int32;
        }
    }
}
