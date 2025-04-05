using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Patch.Reader
{
    public unsafe class HlNativeOpCodeReader( HL_opcode* opcodes, int nops ) : HlOpCodeReader
    {
        private int pIndex = 0;
        public override bool IsEmpty => nops == 0;
        public override bool MoveNext()
        {
            if (nops <= 0)
            {
                return false;
            }
            nops--;
            opcodes++;
            pIndex = 0;
            return true;
        }

        public override int Read(int operandCount)
        {
            var cur = opcodes;
            if (operandCount >= 0)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(pIndex, operandCount + 1);
            }
            return pIndex++ switch
            {
                0 => (int) cur->op,
                1 => cur->p1,
                2 => cur->p2,
                3 => cur->p3,
                > 3 => operandCount == 4 ? (int)cur->extra : cur->extra[pIndex - 3],
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
