using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Data.DFA
{
    internal class SSARegisterData
    {
        public bool IsRefExposed => reg?.IsExposed ?? false;
        public bool IsExposed => IsRefExposed || crossBB;
        public bool crossBB;
        public bool isLast;
        public IRResult overrideValue = new();
        public HlFuncRegisterData? reg;

    }
}
