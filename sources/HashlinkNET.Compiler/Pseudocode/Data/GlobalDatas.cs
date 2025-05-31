using HashlinkNET.Bytecode;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Data
{
    record class FuncEmitGlobalData(
        HlFunction Function,
        HlTypeFun FuncType,
        MethodDefinition Definition
        )
    {

        public List<HlBasicBlockData> HlBasicBlocks
        {
            get;
        } = [];
        public List<IRBasicBlockData> IRBasicBlocks
        {
            get;
        } = [];
        public List<HlFuncRegisterData?> Registers
        {
            get;
        } = [];
    }
}
