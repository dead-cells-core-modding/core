using HashlinkNET.Compiler.Pseudocode.IR;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Ref
{
    class IR_RefArray(
        IRResult src,
        IRResult index,
        TypeReference type
        ) : IRBase(src, index)
    {
        public readonly IRResult src = src;
        public readonly IRResult index = index;
        public TypeReference refType = type;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            index.Emit(ctx, true);
            il.Emit(OpCodes.Ldelema, ((ByReferenceType) refType).ElementType);
            return refType;
        }
    }
}
