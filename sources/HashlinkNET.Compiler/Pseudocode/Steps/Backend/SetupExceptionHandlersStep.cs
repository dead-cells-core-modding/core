using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

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
            if (gdata.TrapRegions.Count == 0)
            {
                return;
            }
            var gdata2 = container.GetGlobalData<GlobalData>();
            var rdata = container.GetGlobalData<RuntimeImports>();
            var md = gdata.Definition;
            HashlinkMethodMarker.MarkShouldUseNative(container, md);
            // Use the linearized list (from LinearizeBasicBlocksStep) because
            // it reflects the actual instruction order after BB cloning.
            var linearList = container.GetGlobalData<List<IRBasicBlockData>>();

            foreach (var tr in gdata.TrapRegions)
            {
                // Malformed/nested trap information can produce a handler end
                // that precedes the catch entry. Emitting such a region would
                // produce invalid IL, so skip it.
                if (tr.HandlerEndPosition >= 0 &&
                    tr.HandlerEndPosition < tr.CatchHandlerPosition)
                {
                    continue;
                }

                // --- Resolve BBs from the linearized list ---
                // Use the linearized list because it has the actual startInst/endInst
                // that were placed in the IL stream during EmitILStep.
                // EndTrap/Catch opcodes emit no IR, so they live inside the
                // basic block that precedes the next block boundary. Resolve
                // regions by containment instead of exact BB start matches.
                IRBasicBlockData? FindContaining( int opIndex )
                {
                    return linearList.FirstOrDefault(b =>
                        b.startInHlbc <= opIndex && opIndex < b.endInHlbc);
                }

                var tryEndBB = FindContaining(tr.TryEndPosition);
                var handlerBB = linearList.Find(b => b.startInHlbc == tr.CatchHandlerPosition)
                    ?? FindContaining(tr.CatchHandlerPosition);

                if (tryEndBB == null || handlerBB == null)
                {
                    continue;
                }

                IRBasicBlockData? handlerEndBB = null;
                if (tr.HandlerEndPosition >= 0)
                {
                    handlerEndBB = FindContaining(tr.HandlerEndPosition);
                    if (handlerEndBB == null)
                    {
                        continue;
                    }
                }

                // The try body starts immediately after the Trap opcode.
                var tryStartBB =
                    linearList.Find(b => b.startInHlbc == tr.TrapOpcodePosition + 1) ??
                    linearList.FirstOrDefault(b => b.startInHlbc > tr.TrapOpcodePosition);

                // --- Instruction boundaries ---
                Instruction tryStart = tryStartBB?.startInst ?? tryEndBB.startInst;
                Instruction tryEnd = tryEndBB.endInst;
                Instruction handlerStart = handlerBB.startInst;
                Instruction handlerEnd = handlerEndBB?.endInst ?? handlerBB.endInst;

                // A trap region that cannot be expressed as a CLR protected
                // region is dropped before any handler/branch rewriting takes
                // place.  Cloning instructions to make illegal entry edges
                // legal corrupts partially-evaluated expressions and leaves
                // unbalanced stack states behind, so unsupported regions are
                // simply left as ordinary control flow instead.
                if (!CanRepresentRegion(md, tryStart, tryEnd, handlerStart, handlerEnd))
                {
                    continue;
                }

                // --- Store caught exception into the trap's destination register ---
                // .NET pushes the exception onto the stack when entering a catch handler.
                // The HL VM places it in the trap's dst register automatically;
                // we must emit a store (or at minimum a pop) to consume it.
                var excReg = gdata.Registers[tr.ExceptionRegIndex];
                var il = md.Body.GetILProcessor();
                if (excReg != null &&
                    (excReg.Variable != null || excReg.Parameter != null))
                {
                    // Convert the managed exception back into the HL value that
                    // the translated code expects in the exception register.
                    var unwrap = Instruction.Create(OpCodes.Call,
                        rdata.phUnwrapException.MakeInstance(excReg.RegisterType));
                    il.InsertAfter(handlerStart, unwrap);
                    il.InsertAfter(unwrap,
                        excReg.Variable != null
                            ? Instruction.Create(OpCodes.Stloc, excReg.Variable)
                            : Instruction.Create(OpCodes.Starg, excReg.Parameter));
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

            // --- Second pass: enforce .NET SEH branch rules ---
            FixLeaveTargets(md);
            FixCrossBoundaryBranches(md);

            // --- Final cleanup ---
            // The recursive clone pass may have placed Leave instructions
            // outside any protected region.  Convert them back to Br.
            FixLeavesOutsideTry(md);
        }

        /// <summary>
        /// Convert any <c>Leave</c>/<c>Leave_S</c> that is NOT inside a try
        /// region back to <c>Br</c>.  The CLR treats Leave-outside-try as Br,
        /// but PEVerify warns about it.
        /// </summary>
        private static void FixLeavesOutsideTry( MethodDefinition md )
        {
            foreach (var inst in md.Body.Instructions)
            {
                if (inst.OpCode != OpCodes.Leave
                    && inst.OpCode != OpCodes.Leave_S) continue;

                bool insideAnyTry = md.Body.ExceptionHandlers.Any(
                    eh => IsBetween(inst, eh.TryStart, eh.TryEnd));

                if (!insideAnyTry)
                {
                    inst.OpCode = OpCodes.Br;
                }
            }
        }

        /// <summary>
        /// .NET SEH requires that every exit from a protected region uses
        /// <c>Leave</c> (or <c>Leave_S</c>) instead of a plain branch.
        /// Walk every instruction in the method body and convert any
        /// branch whose source is inside a try and whose target is outside
        /// to <c>Leave</c>.
        /// </summary>
        private static void FixCrossBoundaryBranches( MethodDefinition md )
        {
            foreach (var eh in md.Body.ExceptionHandlers)
            {
                if (eh.TryEnd == null) continue;

                foreach (var inst in md.Body.Instructions.ToArray())
                {
                    // Only handle unconditional branches (Br / Br_S).
                    if (inst.OpCode != OpCodes.Br
                        && inst.OpCode != OpCodes.Br_S) continue;

                    var target = (Instruction)inst.Operand;
                    bool srcInTry = IsBetween(inst, eh.TryStart, eh.TryEnd);
                    bool tgtInTry = IsBetween(target, eh.TryStart, eh.TryEnd);

                    if (srcInTry && !tgtInTry)
                    {
                        // Exit from try must use Leave.
                        inst.OpCode = OpCodes.Leave;
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether a trap region can be represented as a CLR protected
        /// region without cloning instructions.  Branches from before the try
        /// (or between the try and its handler) into the try are allowed
        /// because <see cref="NormalizeExceptionRegionsStep"/> extends the
        /// region to cover them.  Any other illegal entry edge means the
        /// region must be left untranslated.
        /// </summary>
        private static bool CanRepresentRegion(
            MethodDefinition md,
            Instruction tryStart,
            Instruction tryEnd,
            Instruction handlerStart,
            Instruction handlerEnd )
        {
            var instructions = md.Body.Instructions;
            var tryStartIdx = instructions.IndexOf(tryStart);
            var tryEndIdx = instructions.IndexOf(tryEnd);
            var handlerStartIdx = instructions.IndexOf(handlerStart);
            var handlerEndIdx = instructions.IndexOf(handlerEnd);
            if (tryStartIdx < 0 || tryEndIdx < 0 || handlerStartIdx < 0 ||
                tryEndIdx <= tryStartIdx ||
                (handlerEndIdx >= 0 && handlerEndIdx <= handlerStartIdx))
            {
                return false;
            }

            bool InRange( int index, int start, int end ) =>
                index >= start && (end < 0 ? index < instructions.Count : index < end);

            for (var i = 0; i < instructions.Count; i++)
            {
                var ins = instructions[i];
                if (ins.Operand is Instruction target)
                {
                    if (!IsRepresentableEdge(i, instructions.IndexOf(target)))
                    {
                        return false;
                    }
                }
                else if (ins.Operand is Instruction[] targets)
                {
                    foreach (var t in targets)
                    {
                        if (!IsRepresentableEdge(i, instructions.IndexOf(t)))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;

            bool IsRepresentableEdge( int srcIdx, int tgtIdx )
            {
                if (tgtIdx < 0)
                {
                    return true;
                }

                var srcTry = InRange(srcIdx, tryStartIdx, tryEndIdx);
                var tgtTry = InRange(tgtIdx, tryStartIdx, tryEndIdx);
                var srcHandler = InRange(srcIdx, handlerStartIdx, handlerEndIdx);
                var tgtHandler = InRange(tgtIdx, handlerStartIdx, handlerEndIdx);

                if (tgtHandler && !srcHandler)
                {
                    // Handlers are entered by the runtime only.
                    return false;
                }
                if (srcTry && tgtHandler)
                {
                    return false;
                }
                if (srcHandler && tgtTry)
                {
                    return false;
                }
                if (!srcTry && tgtTry)
                {
                    // Entering a try from outside is only representable when
                    // the source can be folded into the region.
                    return srcIdx < tryStartIdx ||
                        (srcIdx >= tryEndIdx && srcIdx < handlerStartIdx);
                }
                return true;
            }
        }

        /// <summary>
        /// Walk all Leave instructions in the method body and redirect any whose
        /// target falls inside an exception handler so that they point to the
        /// instruction just after the handler instead.
        /// </summary>
        internal static void FixLeaveTargets( MethodDefinition md )
        {
            var instructions = md.Body.Instructions;
            for (var pass = 0; pass < 8; pass++)
            {
                var changed = false;
                foreach (var inst in instructions)
                {
                    if (inst.OpCode != OpCodes.Leave &&
                        inst.OpCode != OpCodes.Leave_S)
                    {
                        continue;
                    }

                    var target = (Instruction)inst.Operand;
                    foreach (var eh in md.Body.ExceptionHandlers)
                    {
                        if (eh.HandlerEnd == null)
                        {
                            continue;
                        }
                        if (IsBetween(target, eh.HandlerStart, eh.HandlerEnd))
                        {
                            inst.Operand = eh.HandlerEnd;
                            changed = true;
                            break;
                        }
                    }
                }
                if (!changed)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Returns true if <paramref name="inst"/> lies strictly between
        /// <paramref name="start"/> (inclusive) and <paramref name="end"/> (exclusive)
        /// in the instruction stream.
        /// </summary>
        private static bool IsBetween(
            Instruction inst, Instruction start, Instruction end )
        {
            if (inst == start) return true;
            var cur = start.Next;
            while (cur != null && cur != end)
            {
                if (cur == inst) return true;
                cur = cur.Next;
            }
            return false;
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
