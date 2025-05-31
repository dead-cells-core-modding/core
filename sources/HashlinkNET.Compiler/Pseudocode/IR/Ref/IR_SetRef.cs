using HashlinkNET.Compiler.Pseudocode.IR;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Ref
{
    class IR_SetRef(
        IRResult src,
        IRResult val
        ) : IRBase(src, val)
    {
        public readonly IRResult src = src;
        public readonly IRResult val = val;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            var vt = val.Emit(ctx, true);
            il.Emit(OpCodes.Stobj, vt);
            return null;
        }
    }
}
