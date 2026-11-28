using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    /// <summary>
    /// Removes no-op instructions after exception handlers have been created.
    /// Branch targets and protected-region boundaries are redirected to the
    /// next real instruction. Some JIT versions reject branch targets that
    /// only lead through nop padding at the end of a protected block.
    /// </summary>
    internal class RemoveNopILStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var body = gdata.Definition.Body;
            if (body.Instructions.Count == 0)
            {
                return;
            }

            // Index instructions once so protected-region tests are O(1)
            // instead of walking the instruction linked list. Only needed when
            // the method actually has exception handlers.
            Dictionary<Instruction, int>? index = null;
            if (body.ExceptionHandlers.Count > 0)
            {
                index = new Dictionary<Instruction, int>(body.Instructions.Count);
                for (var i = 0; i < body.Instructions.Count; i++)
                {
                    index[body.Instructions[i]] = i;
                }
            }

            bool Inside( Instruction inst, Instruction? start, Instruction? end )
            {
                if (index is null || start is null || end is null)
                {
                    return false;
                }
                if (inst == start)
                {
                    return true;
                }
                if (!index.TryGetValue(inst, out var i) ||
                    !index.TryGetValue(start, out var s))
                {
                    return false;
                }
                var e = index.TryGetValue(end, out var endIndex)
                    ? endIndex
                    : body.Instructions.Count;
                return i >= s && i < e;
            }

            // Collapse chains of unconditional branches that ultimately land
            // on the instruction immediately following the chain.  Leaving
            // them in place would turn a plain fallthrough into a branch that
            // enters a protected region from outside.
            Instruction? ResolveJumpChain( Instruction? inst )
            {
                var guard = 0;
                while (inst != null && guard++ < 64)
                {
                    if (inst.OpCode.Code == Code.Nop)
                    {
                        inst = inst.Next;
                        continue;
                    }
                    if (inst.OpCode.Code is Code.Br or Code.Br_S &&
                        inst.Operand is Instruction next)
                    {
                        inst = next;
                        continue;
                    }
                    break;
                }
                return inst;
            }

            for (var pass = 0; pass < 8; pass++)
            {
                var changed = false;
                foreach (var ins in body.Instructions)
                {
                    if (ins.OpCode.Code is not (Code.Br or Code.Br_S) ||
                        ins.Operand is not Instruction target)
                    {
                        continue;
                    }
                    var inRegion = false;
                    foreach (var eh in body.ExceptionHandlers)
                    {
                        if (Inside(ins, eh.TryStart, eh.TryEnd) ||
                            Inside(ins, eh.HandlerStart, eh.HandlerEnd))
                        {
                            inRegion = true;
                            break;
                        }
                    }
                    if (inRegion)
                    {
                        continue;
                    }
                    if (ResolveJumpChain(ins.Next) == ResolveJumpChain(target))
                    {
                        ins.OpCode = OpCodes.Nop;
                        ins.Operand = null;
                        changed = true;
                    }
                }
                if (!changed)
                {
                    break;
                }
            }

            var nops = new List<Instruction>();
            foreach (var ins in body.Instructions)
            {
                if (ins.OpCode.Code == Code.Nop)
                {
                    nops.Add(ins);
                }
            }
            if (nops.Count == 0)
            {
                return;
            }

            var next = new Dictionary<Instruction, Instruction?>();
            foreach (var nop in nops)
            {
                var n = nop.Next;
                while (n != null && n.OpCode.Code == Code.Nop)
                {
                    n = n.Next;
                }
                next[nop] = n;
            }

            Instruction? Map( Instruction? inst )
            {
                if (inst == null)
                {
                    return null;
                }
                var guard = 0;
                while (next.TryGetValue(inst, out var n) && n != null && guard++ < 64)
                {
                    inst = n;
                }
                return inst;
            }

            foreach (var ins in body.Instructions)
            {
                if (ins.Operand is Instruction target)
                {
                    ins.Operand = Map(target) ?? ins.Operand;
                }
                else if (ins.Operand is Instruction[] targets)
                {
                    for (var i = 0; i < targets.Length; i++)
                    {
                        targets[i] = Map(targets[i]) ?? targets[i];
                    }
                }
            }

            foreach (var eh in body.ExceptionHandlers)
            {
                eh.TryStart = Map(eh.TryStart) ?? eh.TryStart;
                eh.TryEnd = Map(eh.TryEnd);
                eh.HandlerStart = Map(eh.HandlerStart);
                eh.HandlerEnd = Map(eh.HandlerEnd);
                eh.FilterStart = Map(eh.FilterStart);
            }

            // Rebuild the instruction list in one pass instead of calling
            // Remove() per nop (which is O(n) each).
            var nopSet = new HashSet<Instruction>(nops);
            var kept = new List<Instruction>(body.Instructions.Count - nops.Count);
            foreach (var ins in body.Instructions)
            {
                if (!nopSet.Contains(ins))
                {
                    kept.Add(ins);
                }
            }
            body.Instructions.Clear();
            foreach (var ins in kept)
            {
                body.Instructions.Add(ins);
            }

            if (HasIllegalExceptionEdges(body))
            {
                NormalizeExceptionRegionsStep.DropExceptionRegions(body);
            }
        }

        private static bool HasIllegalExceptionEdges( MethodBody body )
        {
            if (body.ExceptionHandlers.Count == 0)
            {
                return false;
            }

            var index = new Dictionary<Instruction, int>();
            for (var i = 0; i < body.Instructions.Count; i++)
            {
                index[body.Instructions[i]] = i;
            }

            bool Inside( int i, Instruction? start, Instruction? end )
            {
                if (start == null || !index.TryGetValue(start, out var s))
                {
                    return false;
                }
                var e = end == null || !index.TryGetValue(end, out var endIndex)
                    ? body.Instructions.Count
                    : endIndex;
                return i >= s && i < e;
            }

            foreach (var ins in body.Instructions)
            {
                var i = index[ins];
                foreach (var eh in body.ExceptionHandlers)
                {
                    var inTry = Inside(i, eh.TryStart, eh.TryEnd);
                    var inHandler = Inside(i, eh.HandlerStart, eh.HandlerEnd);
                    if (ins.OpCode.Code == Code.Ret && (inTry || inHandler))
                    {
                        return true;
                    }

                    if (ins.Operand is not Instruction target ||
                        !index.TryGetValue(target, out var ti))
                    {
                        continue;
                    }

                    var tgtTry = Inside(ti, eh.TryStart, eh.TryEnd);
                    var tgtHandler = Inside(ti, eh.HandlerStart, eh.HandlerEnd);
                    var isLeave = ins.OpCode.Code is Code.Leave or Code.Leave_S;
                    if (inTry && !tgtTry && !isLeave)
                    {
                        return true;
                    }
                    if (!inTry && tgtTry)
                    {
                        return true;
                    }
                    if (tgtHandler && !inHandler)
                    {
                        return true;
                    }
                    if (inHandler && !tgtHandler && !isLeave)
                    {
                        return true;
                    }
                    if (isLeave && (tgtTry || tgtHandler))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
