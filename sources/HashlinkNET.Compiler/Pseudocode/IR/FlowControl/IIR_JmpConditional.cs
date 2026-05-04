using HashlinkNET.Compiler.Pseudocode.Data;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    internal interface IIR_JmpConditional
    {
        void ReserveCondition();
        IRBasicBlockData Target {
            get; set;
        }
    }
}
