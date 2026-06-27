using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using System.Collections;
using System.Diagnostics;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    internal class LinearizeBasicBlocksStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();

            var list = container.AddGlobalData<List<IRBasicBlockData>>();

            if (gdata.IRBasicBlocks.Count == 0)
            {
                return;
            }

            Queue<IRBasicBlockData> queue = [];
            Queue<IRBasicBlockData> highQueue = [];
            BitArray visited = new(gdata.IRBasicBlocks.Count);



            // --- Pass 1: normal control-flow BFS (entry + reachable BBs) ---
            highQueue.Enqueue(gdata.IRBasicBlocks[0]);

            void RunBFS()
            {
                while (highQueue.TryDequeue(out var bb) ||
                       queue.TryDequeue(out bb))
                {
                    if (bb.index >= 0)
                    {
                        if (visited[bb.index])
                        {
                            continue;
                        }
                        visited[bb.index] = true;
                    }
                    else
                    {
                        bb.index = -2;
                    }

                    list.Add(bb);

                    Debug.Assert(bb.defaultTransition != null || bb.transitions.Count == 0);

                    foreach (var v in bb.transitions)
                    {
                        if (v.Kind == TransitionKind.Default)
                        {
                            highQueue.Enqueue(v.Target);
                        }
                        else
                        {
                            queue.Enqueue(v.Target);
                        }
                    }
                }
            }

            visited.SetAll(false);
            RunBFS();

            // --- Pass 2: handler BBs ---
            // .NET requires the handler to be at a higher IL offset than the
            // protected region.  Defer handler BBs until after all normal-flow
            // BBs have been emitted so that HandlerStart > TryEnd.
            foreach (var tr in gdata.TrapRegions)
            {
                var handlerBB = gdata.IRBasicBlocks.Find(
                    b => b.startInHlbc == tr.CatchHandlerPosition);
                if (handlerBB != null && handlerBB.index >= 0 && !visited[handlerBB.index])
                {
                    highQueue.Enqueue(handlerBB);
                }
            }
            RunBFS();
        }
    }
}
