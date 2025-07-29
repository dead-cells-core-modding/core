using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.IR.FlowControl;
using HashlinkNET.Compiler.Steps;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    internal class LinearizeBasicBlocksStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();

            var list = container.AddGlobalData<List<IRBasicBlockData>>();

            Queue<IRBasicBlockData> queue = [];
            Queue<IRBasicBlockData> highQueue = [];
            BitArray visited = new(gdata.IRBasicBlocks.Count);

            highQueue.Enqueue(gdata.IRBasicBlocks[0]);
            visited.SetAll(false);
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
    }
}
