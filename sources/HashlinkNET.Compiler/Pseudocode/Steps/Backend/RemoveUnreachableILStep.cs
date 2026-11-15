using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    /// <summary>
    /// Removes instructions that are unreachable from the method entry.
    /// Leftover instructions after a terminator can confuse the runtime JIT
    /// when they are partially targeted by branches.
    /// </summary>
    internal class RemoveUnreachableILStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var md = gdata.Definition;
            var body = md.Body;

            // Skip methods with exception handlers: their regions require
            // precise instruction ranges to stay intact.
            if (body.ExceptionHandlers.Count > 0 ||
                body.Instructions.Count == 0)
            {
                return;
            }

            var index = new Dictionary<Instruction, int>(body.Instructions.Count);
            for (var i = 0; i < body.Instructions.Count; i++)
            {
                index[body.Instructions[i]] = i;
            }

            var reachable = new bool[body.Instructions.Count];
            var queue = new Queue<Instruction>();
            queue.Enqueue(body.Instructions[0]);

            void Push( Instruction? target )
            {
                if (target != null && index.ContainsKey(target))
                {
                    queue.Enqueue(target);
                }
            }

            while (queue.Count > 0)
            {
                var inst = queue.Dequeue();
                var i = index[inst];
                if (reachable[i])
                {
                    continue;
                }
                reachable[i] = true;

                var code = inst.OpCode.Code;
                switch (code)
                {
                    case Code.Ret:
                    case Code.Throw:
                    case Code.Rethrow:
                    case Code.Jmp:
                        continue;
                    case Code.Br:
                    case Code.Br_S:
                    case Code.Leave:
                    case Code.Leave_S:
                        Push(inst.Operand as Instruction);
                        continue;
                }

                if (inst.Operand is Instruction target)
                {
                    Push(target);
                }
                else if (inst.Operand is Instruction[] targets)
                {
                    foreach (var t in targets)
                    {
                        Push(t);
                    }
                }
                Push(inst.Next);
            }

            var keep = new List<Instruction>(body.Instructions.Count);
            for (var i = 0; i < body.Instructions.Count; i++)
            {
                if (reachable[i])
                {
                    keep.Add(body.Instructions[i]);
                }
            }

            if (keep.Count == body.Instructions.Count)
            {
                return;
            }

            body.Instructions.Clear();
            foreach (var inst in keep)
            {
                body.Instructions.Add(inst);
            }
        }
    }
}
