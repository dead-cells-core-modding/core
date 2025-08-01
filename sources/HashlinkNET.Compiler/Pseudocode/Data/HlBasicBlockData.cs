using HashlinkNET.Bytecode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Data
{
    record class HlBasicBlockData
    {
        public readonly List<Transition> transitions = [];
        public required int opcodeStart;
        public required Memory<HlOpcode> opcodes;
        public required HlFunction function;
        public record class Transition
        (
            HlBasicBlockData Target,
            HlOpcode BindingOpCode,
            TransitionKind Kind
        );
    }
}
