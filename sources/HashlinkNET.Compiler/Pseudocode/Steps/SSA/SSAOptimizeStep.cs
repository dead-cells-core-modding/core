using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.IR;
using HashlinkNET.Compiler.Pseudocode.IR.SSA;
using HashlinkNET.Compiler.Steps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps.SSA
{
    internal class SSAOptimizeStep : CompileStep
    {
        class LocalRegAccessScope
        {
            
        }
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var md = gdata.Definition;
            var bbs = gdata.IRBasicBlocks;
            foreach (var bb in bbs)
            {
                var ss = new Stack<LocalRegAccessScope>();
                foreach (var ir in bb.flatIR!)
                {
                    if (ir.IR is IR_SSA_Save sa)
                    {
                        if (sa.dst.IsRefExposed)
                        {
                            continue;
                        }
                        if (sa.value.IR is IR_LoadConst lc)
                        {
                            sa.dst.overrideValue = lc;
                        }
                    }
                }
            }
        }
    }
}
