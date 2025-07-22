using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Mem
{
    internal class IR_SetMem(
        IRResult ptr,
        IRResult index,
        IRResult val
        ) : IRBase(ptr, index, val)
    {
        public readonly IRResult ptr = ptr;
        public readonly IRResult val = val;
        public readonly IRResult index = index;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            ptr.Emit(ctx, true);

            index.Emit(ctx, true);

            var vt = val.Emit(ctx, true)!;

            il.Emit(OpCodes.Call, ctx.RuntimeImports.phWriteMem.MakeInstance(vt));
            return null;
        }
    }
}
