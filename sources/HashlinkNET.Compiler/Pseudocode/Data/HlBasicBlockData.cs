using HashlinkNET.Bytecode;

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
