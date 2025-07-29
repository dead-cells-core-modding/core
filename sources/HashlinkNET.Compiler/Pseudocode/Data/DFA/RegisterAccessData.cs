using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Data.DFA
{
    class RegisterAccessData(int regCount)
    {
        public readonly BitArray firstReadReg = new(regCount);
        public readonly BitArray firstWriteReg = new(regCount);
        public readonly BitArray writeReg = new(regCount);
        public readonly BitArray readReg = new(regCount);
        public readonly BitArray requireBySelfAndChildrenReg = new(regCount);
        public readonly BitArray exposedReg = new(regCount);

        public readonly List<int> regAccess = [];
        public SSARegisterData[] ssaRegisters = [];
    }
}
