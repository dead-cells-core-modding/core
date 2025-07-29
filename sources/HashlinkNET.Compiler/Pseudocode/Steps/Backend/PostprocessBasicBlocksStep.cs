using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.IR.FlowControl;
using HashlinkNET.Compiler.Steps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    internal class PostprocessBasicBlocksStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();

            foreach (var bb in gdata.IRBasicBlocks)
            {
                if (
                    bb.transitions.Count != 2 ||
                    bb.ir.Count == 0
                    )
                {
                    continue;
                }
                if (bb.ir[^1].IR is not IIR_JmpConditional jmp)
                {
                    continue;
                }

                Debug.Assert(bb.defaultTransition != null);

                var defaultTarget = bb.defaultTransition;
                var condTarget = jmp.Target;
                if (!defaultTarget.CanRepeat)
                {
                    if (condTarget.CanRepeat)
                    {
                        bb.ReserveConditionalJmp();
                    }
                }
            }

            for (int i = 0; i < gdata.IRBasicBlocks.Count; i++)
            {
                var bb = gdata.IRBasicBlocks[i];
                if (bb.ir.Count == 0)
                {
                    continue;
                }
                for (int j = 0; j < bb.transitions.Count; j++)
                {
                    var v = bb.transitions[j];
                    if (v.Kind == TransitionKind.Default)
                    {
                        if (v.Target.CanRepeat)
                        {
                            var newbb = v.Target.Clone();
                            newbb.index = -1;

                            bb.defaultTransition = newbb;
                            gdata.IRBasicBlocks.Add(newbb);

                            bb.transitions[j] = new(newbb, TransitionKind.Default);

                            if (bb.ir[^1].IR is IR_Jmp jmp)
                            {
                                jmp.target = newbb;
                            }
                        }
                        break;
                    }
                }
            }

        }
    }
}
