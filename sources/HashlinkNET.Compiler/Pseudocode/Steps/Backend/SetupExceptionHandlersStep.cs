using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    /// <summary>
    /// Creates Mono.Cecil <see cref="ExceptionHandler"/> entries for try-catch regions.
    /// Runs after <see cref="EmitILStep"/> has emitted IL so that
    /// <see cref="Instruction"/> boundary references are available.
    /// </summary>
    internal class SetupExceptionHandlersStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var gdata2 = container.GetGlobalData<GlobalData>();
            var md = gdata.Definition;
            // Use the linearized list (from LinearizeBasicBlocksStep) because
            // it reflects the actual instruction order after BB cloning.
            var linearList = container.GetGlobalData<List<IRBasicBlockData>>();

            foreach (var tr in gdata.TrapRegions)
            {
                // --- Resolve BBs from the linearized list ---
                // Use the linearized list because it has the actual startInst/endInst
                // that were placed in the IL stream during EmitILStep.
                var tryBBs = linearList.Where(b =>
                    b.startInHlbc > tr.TrapOpcodePosition
                    && b.startInHlbc <= tr.TryEndPosition).ToList();

                var tryEndBB = linearList.Find(b => b.startInHlbc == tr.TryEndPosition);
                var handlerBB = linearList.Find(b => b.startInHlbc == tr.CatchHandlerPosition);

                if (tryEndBB == null || handlerBB == null)
                    continue;

                IRBasicBlockData? handlerEndBB = null;
                if (tr.HandlerEndPosition >= 0)
                {
                    handlerEndBB = linearList.Find(
                        b => b.startInHlbc == tr.HandlerEndPosition);
                }

                // --- Instruction boundaries ---
                Instruction tryStart = tryBBs.Count > 0
                    ? tryBBs[0].startInst
                    : tryEndBB.startInst;
                Instruction tryEnd = tryEndBB.endInst;
                Instruction handlerStart = handlerBB.startInst;
                Instruction handlerEnd = handlerEndBB?.endInst ?? handlerBB.endInst;

                // --- Store caught exception into the trap's destination register ---
                // .NET pushes the exception onto the stack when entering a catch handler.
                // The HL VM places it in the trap's dst register automatically;
                // we must emit a store (or at minimum a pop) to consume it.
                var excReg = gdata.Registers[tr.ExceptionRegIndex];
                var il = md.Body.GetILProcessor();
                if (excReg?.Variable != null)
                {
                    il.InsertAfter(handlerStart,
                        Instruction.Create(OpCodes.Stloc, excReg.Variable));
                }
                else if (excReg?.Parameter != null)
                {
                    il.InsertAfter(handlerStart,
                        Instruction.Create(OpCodes.Starg, excReg.Parameter));
                }
                else
                {
                    // Register not found — still need to consume the exception from
                    // the stack to keep it balanced.
                    il.InsertAfter(handlerStart, Instruction.Create(OpCodes.Pop));
                }

                // --- Replace Br with Leave ---
                // .NET requires Leave (not Br) to exit a protected region.
                ReplaceBrWithLeave(tryEndBB);

                // If the handler's closing endtrap BB falls through to a block
                // outside the handler, replace its Br with Leave as well.
                if (handlerEndBB != null)
                {
                    ReplaceBrWithLeave(handlerEndBB);
                }

                // --- Resolve exception type ---
                // Mono.Cecil requires a non-null CatchType. Use System.Object for
                // catch-all, which is semantically equivalent (all exceptions inherit
                // from System.Object in .NET).
                TypeReference catchType = gdata2.Module.TypeSystem.Object;
                if (tr.CatchTypeGlobalIndex.HasValue)
                {
                    var hlc = gdata2.Code;
                    var globalIndex = tr.CatchTypeGlobalIndex.Value;
                    if (globalIndex > 0 && globalIndex < hlc.Globals.Count)
                    {
                        var hlType = hlc.Globals[globalIndex].Value;
                        catchType = container.GetTypeRef(hlType)
                            ?? gdata2.Module.TypeSystem.Object;
                    }
                }

                // Store resolved references for diagnostics
                tr.TryStart = tryStart;
                tr.TryEnd = tryEnd;
                tr.HandlerStart = handlerStart;
                tr.HandlerEnd = handlerEnd;
                tr.CatchType = catchType;

                // --- Create Mono.Cecil ExceptionHandler ---
                md.Body.ExceptionHandlers.Add(new ExceptionHandler(
                    ExceptionHandlerType.Catch)
                {
                    TryStart = tryStart,
                    TryEnd = tryEnd,
                    HandlerStart = handlerStart,
                    HandlerEnd = handlerEnd,
                    CatchType = catchType,
                });
            }
        }

        /// <summary>
        /// Replace the <c>Br</c> (or <c>Br_S</c>) at the end of a basic block with
        /// <c>Leave</c> (or <c>Leave_S</c>).  .NET requires a leave instruction to
        /// exit a protected region; a plain branch would fail PEVerify.
        /// </summary>
        private static void ReplaceBrWithLeave( IRBasicBlockData bb )
        {
            var it = bb.endInst.Previous;
            while (it != null && it != bb.startInst)
            {
                if (it.OpCode == OpCodes.Br || it.OpCode == OpCodes.Br_S)
                {
                    // Always use the long form (Leave) to avoid issues with
                    // offset overflows after handler-entry stloc insertion etc.
                    it.OpCode = OpCodes.Leave;
                    return;
                }
                it = it.Previous;
            }
        }
    }
}
