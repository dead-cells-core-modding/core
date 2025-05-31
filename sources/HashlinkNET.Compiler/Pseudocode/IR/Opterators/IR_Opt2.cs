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
    class IR_Opt2(
        IRResult srcA,
        IRResult srcB,
        IR_Opt2.OptKind kind
        ) : IRBase(srcA, srcB)
    {
        public enum OptKind
        {
            Add,
            Sub,
            Mul,
            SDiv,
            UDiv,
            SMod,
            UMod,
            Shl,
            SShr,
            UShr,
            And,
            Or,
            Xor,
        }
        public OptKind kind = kind;
        public readonly IRResult srcA = srcA;
        public readonly IRResult srcB = srcB;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var ret = srcA.Emit(ctx, true);
            srcB.Emit(ctx, true);

            il.Emit(kind switch
            {
                OptKind.Add => OpCodes.Add,
                OptKind.And => OpCodes.And,
                OptKind.Mul => OpCodes.Mul,
                OptKind.Or => OpCodes.Or,
                OptKind.SDiv => OpCodes.Div,
                OptKind.Shl => OpCodes.Shl,
                OptKind.SMod => OpCodes.Rem,
                OptKind.SShr => OpCodes.Shr,
                OptKind.Sub => OpCodes.Sub,
                OptKind.UDiv => OpCodes.Div_Un,
                OptKind.UMod => OpCodes.Rem_Un,
                OptKind.UShr => OpCodes.Shr_Un,
                OptKind.Xor => OpCodes.Xor,
                _ => throw new NotSupportedException()
            });
            return ret;
        }
    }
}
