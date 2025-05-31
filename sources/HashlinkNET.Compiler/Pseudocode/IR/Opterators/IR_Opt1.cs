using HashlinkNET.Compiler.Pseudocode.IR;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Opterators
{
    class IR_Opt1(
       IRResult src,
       IR_Opt1.OptKind kind
       ) : IRBase(src)
    {
        public enum OptKind
        {
            Neg,
            Not,
            Incr,
            Decr,
        }
        public OptKind kind = kind;
        public readonly IRResult src = src;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var ret = src.Emit(ctx, true);
            if (kind == OptKind.Neg)
            {
                il.Emit(OpCodes.Neg);
            }
            else if (kind == OptKind.Not)
            {
                il.Emit(OpCodes.Not);
            }
            else if (kind == OptKind.Incr)
            {
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
            }
            else if (kind == OptKind.Decr)
            {
                il.Emit(OpCodes.Ldc_I4, -1);
                il.Emit(OpCodes.Add);
            }
            return ret;
        }
    }
}
