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
                    else if (!srcInTry && tgtInTry)
                    {
                        // Branch into a try from outside is illegal in .NET SEH.
                        // Clone the target block so the back-edge lands outside.
                        CloneTargetOutsideTry(md, eh, inst, target);
                    }
                }
            }
        }

        /// <summary>
        /// Clone a short sequence of instructions starting at
        /// <paramref name="origTarget"/> (which is inside a protected region)
        /// and place the copy just before <c>TryStart</c> so that a back-edge
        /// from outside the try can target the clone instead.
        /// </summary>
        /// 

        private static void CloneTargetOutsideTry(
            MethodDefinition md,
            ExceptionHandler eh,
            Instruction branchInst,
            Instruction origTarget )
        {
            // Collect a short run of non-branch instructions ending at the
            // first branch, ret, or throw.
            var run = new List<Instruction>();
            var it = origTarget;
            const int maxRun = 8;
            while (it != null && run.Count < maxRun)
            {
                run.Add(it);
                var op = it.OpCode;
                if (op == OpCodes.Br || op == OpCodes.Br_S
                    || op == OpCodes.Leave || op == OpCodes.Leave_S
                    || op == OpCodes.Ret || op == OpCodes.Throw
                    || op == OpCodes.Switch
                    || op == OpCodes.Brtrue || op == OpCodes.Brtrue_S
                    || op == OpCodes.Brfalse || op == OpCodes.Brfalse_S
                    || op == OpCodes.Blt || op == OpCodes.Blt_S
                    || op == OpCodes.Bgt || op == OpCodes.Bgt_S
                    || op == OpCodes.Ble || op == OpCodes.Ble_S
                    || op == OpCodes.Bge || op == OpCodes.Bge_S
                    || op == OpCodes.Beq || op == OpCodes.Beq_S
                    || op == OpCodes.Bne_Un || op == OpCodes.Bne_Un_S
                    || op == OpCodes.Blt_Un || op == OpCodes.Blt_Un_S
                    || op == OpCodes.Bgt_Un || op == OpCodes.Bgt_Un_S
                    || op == OpCodes.Ble_Un || op == OpCodes.Ble_Un_S
                    || op == OpCodes.Bge_Un || op == OpCodes.Bge_Un_S)
                    break;
                it = it.Next;
            }
            if (run.Count == 0) return;

            // --- build clone ---
            var cloneFirst = Instruction.Create(OpCodes.Nop);
            var cloneLast = cloneFirst;
            var labelMap = new Dictionary<Instruction, Instruction>();
            foreach (var src in run)
            {
                var c = Instruction.Create(OpCodes.Nop);
                c.OpCode = src.OpCode;
                c.Operand = src.Operand;
                labelMap[src] = c;
                cloneLast.Next = c;
                c.Previous = cloneLast;
                cloneLast = c;
            }
            // Drop the leading Nop we used as anchor
            var firstReal = cloneFirst.Next!;
            if (firstReal != null)
                firstReal.Previous = null;

            // Remap intra-clone branch targets
            foreach (var src in run)
            {
                var c = labelMap[src];
                if (c.Operand is Instruction t && labelMap.TryGetValue(t, out var r))
                    c.Operand = r;
            }

            // --- insert clone before TryStart ---
            var insertBefore = eh.TryStart;
            if (firstReal != null)
            {
                var prev = insertBefore.Previous;
                if (prev != null) prev.Next = firstReal;
                firstReal.Previous = prev;
                cloneLast.Next = insertBefore;
                insertBefore.Previous = cloneLast;

                // Insert into instruction collection
                var idx = md.Body.Instructions.IndexOf(insertBefore);
                foreach (var src in run)
                    md.Body.Instructions.Insert(idx++, labelMap[src]);
            }

            // --- redirect the original branch to the clone ---
            branchInst.Operand = firstReal ?? insertBefore;

            // --- recursively fix any branch in the clone that still
            //     targets inside the try (e.g. the loop-exit condition) ---
            foreach (var src in run)
            {
                var c = labelMap[src];
                if (c.Operand is Instruction t
                    && IsBetween(t, eh.TryStart, eh.TryEnd))
                {
                    // The clone's branch still points inside the try.
                    // Recursively clone that target too.
                    CloneTargetOutsideTry(md, eh, c, t);
                }
            }
        }

        /// <summary>
        /// Walk all Leave instructions in the method body and redirect any whose
        /// target falls inside an exception handler so that they point to the
        /// instruction just after the handler instead.
        /// </summary>
        private static void FixLeaveTargets( MethodDefinition md )
        {
            foreach (var eh in md.Body.ExceptionHandlers)
            {
                if (eh.HandlerEnd == null) continue;

                foreach (var inst in md.Body.Instructions)
                {
                    if (inst.OpCode != OpCodes.Leave
                        && inst.OpCode != OpCodes.Leave_S) continue;

                    var target = (Instruction)inst.Operand;
                    // Does the Leave target fall inside a handler?
                    if (IsBetween(target, eh.HandlerStart, eh.HandlerEnd))
                    {
                        inst.Operand = eh.HandlerEnd;
                        inst.OpCode = OpCodes.Leave;
                    }
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
