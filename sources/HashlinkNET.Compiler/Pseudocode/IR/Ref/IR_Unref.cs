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
    class IR_Unref(
        IRResult src,
        TypeReference type
        ) : IRBase(src)
    {
        public readonly IRResult src = src;
        public readonly TypeReference itemType = type;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            il.Emit(OpCodes.Ldobj, itemType);
            return itemType;
        }
    }
}
