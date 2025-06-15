using HashlinkNET.Compiler.Pseudocode.IR.SSA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Data
{
    internal class SSARegisterData
    {
        public bool IsExposed => reg?.IsExposed ?? false;
        public HlFuncRegisterData? reg;
        public IR_SSA_Save? ir_save;
        public List<IRResult> loadAccess = [];
    }
}
