using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps
{
    class SplitBasicBlocksStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var f = gdata.Function;
            var opcodes = f.Opcodes;
            var bb = gdata.HlBasicBlocks;

            if (opcodes.Length == 0)
            {
                return;
            }

            HashSet<int> bbEdgeUnorder = [];

            //Collect edges

            for (var i = 0; i < opcodes.Length; i++)
            {
                var op = opcodes[i];
                var next_ops = i + 1;
                if (
                    op.Kind <= HlOpcodeKind.JAlways && op.Kind >= HlOpcodeKind.JTrue
                    )
                {
                    //Basic Block Start
                    bbEdgeUnorder.Add(next_ops + op.Parameters[^1]);
                    //Basic Block End
                    bbEdgeUnorder.Add(next_ops);
                }
                else if (op.Kind == HlOpcodeKind.Label)
                {
                    //Basic Block Start
                    bbEdgeUnorder.Add(next_ops);
                }
                else if (op.Kind == HlOpcodeKind.Ret || op.Kind == HlOpcodeKind.Throw)
                {
                    //Basic Block End
                    bbEdgeUnorder.Add(next_ops);
                }
                else if (op.Kind == HlOpcodeKind.Switch)
                {
                    //Basic Block Start
                    //bbEdgeUnorder.Add(i + 1 + op.Parameters.ArrayLastItem());
                    for (var j = 0; j < op.Parameters[1]; j++)
                    {
                        bbEdgeUnorder.Add(next_ops + op.Parameters[3 + j]);
                    }
                    //Basic Block End
                    bbEdgeUnorder.Add(next_ops);
                }
            }

            //Sort
            var bbEdge = bbEdgeUnorder.Order().ToList();

            var start = 0;
            var bbLookup = new Dictionary<int, HlBasicBlockData>();
            foreach (var v in bbEdge)
            {
                if (v >= opcodes.Length)
                {
                    break;
                }
                var bbd = new HlBasicBlockData()
                {
                    opcodeStart = start,
                    opcodes = new Memory<HlOpcode>(opcodes, start, v - start),
                    function = f
                };
                if (bbd.opcodes.IsEmpty)
                {
                    continue;
                }
                bb.Add(bbd);
                bbLookup.Add(start, bbd);
                start = v;
            }
            if (start != opcodes.Length)
            {
                var bbd = new HlBasicBlockData()
                {
                    opcodeStart = start,
                    opcodes = new Memory<HlOpcode>(opcodes, start, opcodes.Length - start),
                    function = f
                };
                bb.Add(bbd);
                bbLookup.Add(start, bbd);
            }

            for (var i = 0; i < bb.Count; i++)
            {
                var bbd = bb[i];

                var lastCode = bbd.opcodes.Span[^1];
                var jmpNext = bbd.opcodeStart + bbd.opcodes.Length;
                if (lastCode.Kind != HlOpcodeKind.JAlways &&
                    lastCode.Kind != HlOpcodeKind.Ret &&
                    lastCode.Kind != HlOpcodeKind.Throw &&
                    i != bb.Count - 1)
                {
                    var target = bb[i + 1];
                    bbd.transitions.Add(
                        new(target, lastCode, TransitionKind.Default)
                        );
                }
                if (lastCode.Kind == HlOpcodeKind.Switch)
                {
                    for (var j = 0; j < lastCode.Parameters[1]; j++)
                    {
                        var target = bbLookup[jmpNext + lastCode.Parameters[3 + j]];
                        bbd.transitions.Add(
                            new(target, lastCode, TransitionKind.Conditional)
                            );
                    }
                }
                if (lastCode.Kind >= HlOpcodeKind.JTrue &&
                    lastCode.Kind <= HlOpcodeKind.JAlways)
                {
                    var target = bbLookup[jmpNext + lastCode.Parameters[^1]];
                    bbd.transitions.Add(
                        new(
                            target, lastCode, 
                            lastCode.Kind == HlOpcodeKind.JAlways ?
                                TransitionKind.Default : TransitionKind.Conditional
                            )
                        );
                }
            }
        }
    }
}
