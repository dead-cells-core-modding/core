using HashlinkNET.Compiler.Pseudocode.Data;
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
                for (var i = bb.ir.Count - (1); i >= 0; i--)
                {
                    var irr = bb.ir[i];
                    var ir = irr.IR;
                    if (ir is IR_SSA_Load sl)
                    {
                        
                    }
                }
            }
        }
    }
}
