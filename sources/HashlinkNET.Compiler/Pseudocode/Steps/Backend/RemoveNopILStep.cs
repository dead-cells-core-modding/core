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
        }
    }
}
