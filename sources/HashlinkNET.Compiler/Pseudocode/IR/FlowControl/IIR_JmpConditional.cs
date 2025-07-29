using HashlinkNET.Compiler.Pseudocode.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    internal interface IIR_JmpConditional
    {
        void ReserveCondition();
        IRBasicBlockData Target
        {
            get; set;
        }
    }
}
