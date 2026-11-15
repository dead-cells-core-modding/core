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
                    if (body.ExceptionHandlers.Any(eh =>
                        IsBetween(ins, eh.TryStart, eh.TryEnd) ||
                        IsBetween(ins, eh.HandlerStart, eh.HandlerEnd)))
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

            var nops = body.Instructions
                .Where(i => i.OpCode.Code == Code.Nop)
                .ToList();
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

            foreach (var nop in nops)
            {
                body.Instructions.Remove(nop);
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

        private static bool IsBetween( Instruction inst, Instruction? start, Instruction? end )
        {
            if (start == null || end == null)
            {
                return false;
            }
            if (inst == start)
            {
                return true;
            }
            var cur = start.Next;
            while (cur != null && cur != end)
            {
                if (cur == inst)
                {
                    return true;
                }
                cur = cur.Next;
            }
            return false;
        }
    }
}
