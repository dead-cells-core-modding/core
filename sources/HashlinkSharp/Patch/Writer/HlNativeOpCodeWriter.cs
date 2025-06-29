using Hashlink.Patch.Reader;
using HashlinkNET.Bytecode;
using HashlinkNET.Bytecode.OpCodeParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Patch.Writer
{
    public unsafe class HlNativeOpCodeWriter(int count) : HlOpCodeWriter
    {
        public readonly HL_opcode[] opcodes = GC.AllocateArray<HL_opcode>(count, true);

        private readonly List<int[]> extraBuffer = [];
        private int[]? curBuffer;
        private int curBufferOffset;
        private HL_opcode* cur = null;
        private int pidx = 0;

        public override void MoveNext( HlOpcodeKind op )
        {
            if (cur == null)
            {
                cur = (HL_opcode*)Unsafe.AsPointer(ref opcodes[0]);
            }
            else
            {
                cur++;
            }
            pidx = 0;
            cur->op = (HL_opcode.OpCodes) op;
        }

        public override void Write( int val, HlOpCode.PayloadKind kind = HlOpCode.PayloadKind.None )
        {
            if (pidx == -1 || cur == null)
            {
                throw new InvalidOperationException();
            }
            if (pidx == 0)
            {
                cur->p1 = val;
            }
            else if (pidx == 1)
            {
                cur->p2 = val;
            }
            else if (pidx == 2)
            {
                cur->p3 = val;
            }
            else
            {
                if (kind.HasFlag(HlOpCode.PayloadKind.ExtraParamPointer))
                {
                    cur->extra = (int*)val;
                    pidx = -1;
                    return;
                }

                if (curBuffer == null)
                {
                    curBuffer = GC.AllocateArray<int>(64, true);
                    curBufferOffset = 0;
                    extraBuffer.Add(curBuffer);
                }

                if (curBuffer.Length == curBufferOffset)
                {
                    var oldEnd = (int*) Unsafe.AsPointer(ref curBuffer[^1]);
                    curBuffer = GC.AllocateArray<int>(64, true);
                    curBufferOffset = 0;
                    extraBuffer.Add(curBuffer);

                    if (cur->extra != null)
                    {
                        var np = (int*) Unsafe.AsPointer(ref curBuffer[0]);
                        while (cur->extra < oldEnd)
                        {
                            *np++ = *cur->extra++;
                            curBufferOffset++;
                        }
                        cur->extra = (int*)Unsafe.AsPointer(ref curBuffer[0]);
                    }
                }

                if (cur->extra == null)
                {
                    cur->extra = (int*) Unsafe.AsPointer(ref curBuffer[curBufferOffset]);
                }

                cur->extra[pidx - 3] = val;
                curBufferOffset++;

            }
            pidx++;
        }
    }
}
