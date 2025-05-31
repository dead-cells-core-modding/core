using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Array
{
    class IR_SetArrayItem(
        IRResult src,
        IRResult index,
        IRResult val
        ) : IRBase(src, index, val)
    {
        public readonly IRResult src = src;
        public readonly IRResult index = index;
        public readonly IRResult val = val;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            index.Emit(ctx, true);
            var rt = val.Emit(ctx, true);
            il.Emit(OpCodes.Stelem_Any, rt);
            return null;
        }
    }
}
