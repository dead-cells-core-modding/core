using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;

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
                else if (op.Kind == HlOpcodeKind.Ret || op.Kind == HlOpcodeKind.Throw ||
                    op.Kind == HlOpcodeKind.EndTrap)
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
                else if (op.Kind == HlOpcodeKind.Trap)
                {
                    // Catch handler entry is a basic block start;
                    // the instruction after trap is also a BB start
                    // (try body begins here, must be a clean boundary
                    //  for exception handler TryStart).
                    bbEdgeUnorder.Add(next_ops + op.Parameters[^1]);
                    bbEdgeUnorder.Add(next_ops);
                }
                else if (op.Kind == HlOpcodeKind.Catch)
                {
                    // Catch opcode itself marks a basic block start
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

            // Second pass: detect trap-catch regions using a stack-based pairing
            DetectTrapRegions(container, f, bb, bbLookup);
        }

        private static void DetectTrapRegions(
            IDataContainer container,
            HlFunction f,
            List<HlBasicBlockData> bb,
            Dictionary<int, HlBasicBlockData> bbLookup )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var opcodes = f.Opcodes;

            // Stack entry for pairing trap/endtrap
            // state: 0 = Trap (waiting for try body endtrap)
            //        1 = Catch (waiting for handler endtrap)
            var trapStack = new Stack<(int trapOpIdx, int regIdx, int targetPos, int state)>();

            foreach (var bbd in bb)
            {
                var ops = bbd.opcodes.Span;
                for (int oi = 0; oi < ops.Length; oi++)
                {
                    var op = ops[oi];
                    int opGlobalIdx = bbd.opcodeStart + oi;

                    if (op.Kind == HlOpcodeKind.Trap)
                    {
                        int regIdx = op.Parameters[0];
                        int offset = op.Parameters[^1];
                        int targetPos = opGlobalIdx + 1 + offset;
                        trapStack.Push((opGlobalIdx, regIdx, targetPos, 0));
                    }
                    else if (op.Kind == HlOpcodeKind.EndTrap)
                    {
                        if (trapStack.Count == 0)
                            continue;

                        var top = trapStack.Peek();
                        if (top.state == 0)
                        {
                            // This EndTrap closes the try body
                            trapStack.Pop();
                            trapStack.Push((top.trapOpIdx, top.regIdx, top.targetPos, 1));

                            // Find or create a pending TrapRegionData
                            // We don't know the HandlerEndPosition yet, so store
                            // a partial entry keyed by TrapOpcodePosition
                            gdata.TrapRegions.Add(new TrapRegionData
                            {
                                ExceptionRegIndex = top.regIdx,
                                TrapOpcodePosition = top.trapOpIdx,
                                CatchHandlerPosition = top.targetPos,
                                TryEndPosition = opGlobalIdx,
                                HandlerEndPosition = -1  // placeholder
                            });
                        }
                        else
                        {
                            // This EndTrap closes the catch handler
                            trapStack.Pop();

                            // Fill in HandlerEndPosition in the matching TrapRegionData
                            var tr = gdata.TrapRegions.FindLast(
                                t => t.TrapOpcodePosition == top.trapOpIdx
                                  && t.HandlerEndPosition == -1);
                            if (tr != null)
                            {
                                tr.HandlerEndPosition = opGlobalIdx;
                            }
                        }
                    }
                    else if (op.Kind == HlOpcodeKind.Catch)
                    {
                        // Modern format: Catch opcode with global index
                        if (trapStack.Count > 0)
                        {
                            var top = trapStack.Peek();
                            int catchTypeGlobalIndex = op.Parameters[0];
                            var tr = gdata.TrapRegions.FindLast(
                                t => t.TrapOpcodePosition == top.trapOpIdx
                                  && t.CatchTypeGlobalIndex == null);
                            if (tr != null)
                            {
                                tr.CatchTypeGlobalIndex = catchTypeGlobalIndex;
                            }
                        }
                    }
                    else if (op.Kind == HlOpcodeKind.GetGlobal && oi + 1 < ops.Length)
                    {
                        // Legacy format (pre-Haxe 5): GetGlobal + Call2 pattern
                        // after the catch label for type checking
                        var nextOp = ops[oi + 1];
                        if (nextOp.Kind == HlOpcodeKind.Call2 && trapStack.Count > 0)
                        {
                            // Check if the Call2 destination matches the exception register
                            int nextGlobalIdx = bbd.opcodeStart + oi + 1;
                            var top = trapStack.Peek();
                            if (top.state == 1)
                            {
                                var tr = gdata.TrapRegions.FindLast(
                                    t => t.TrapOpcodePosition == top.trapOpIdx
                                      && t.CatchTypeGlobalIndex == null);
                                if (tr != null)
                                {
                                    tr.CatchTypeGlobalIndex = op.Parameters[1];
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
