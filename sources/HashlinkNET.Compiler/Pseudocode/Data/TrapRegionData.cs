using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.Data
{
    /// <summary>
    /// Describes a try-catch region discovered during basic block analysis.
    /// Boundaries are first recorded as HL opcode positions (stable across
    /// BB transformations), then resolved to IL <see cref="Instruction"/>
    /// references after IL emission.
    /// </summary>
    internal class TrapRegionData
    {
        /// <summary>Register index where the caught exception value is stored.</summary>
        public required int ExceptionRegIndex {
            get; init;
        }

        /// <summary>
        /// HL global index for the exception type filter.
        /// <c>null</c> means catch-all (no type filter).
        /// </summary>
        public int? CatchTypeGlobalIndex {
            get; set;
        }

        /// <summary>HL opcode position of the <c>Trap</c> instruction.</summary>
        public required int TrapOpcodePosition {
            get; init;
        }

        /// <summary>
        /// HL opcode position of the catch handler entry point
        /// (equal to <c>TrapOpcodePosition + 1 + offset</c>).
        /// </summary>
        public required int CatchHandlerPosition {
            get; init;
        }

        /// <summary>HL opcode position of the <c>EndTrap</c> that closes the try body.</summary>
        public required int TryEndPosition {
            get; set;
        }

        /// <summary>HL opcode position of the <c>EndTrap</c> that closes the catch handler.</summary>
        public required int HandlerEndPosition {
            get; set;
        }

        // Resolved after IL emission (by SetupExceptionHandlersStep):
        public Instruction? TryStart {
            get; set;
        }
        public Instruction? TryEnd {
            get; set;
        }
        public Instruction? HandlerStart {
            get; set;
        }
        public Instruction? HandlerEnd {
            get; set;
        }

        /// <summary>Resolved .NET type for the catch filter, or <c>null</c> for catch-all.</summary>
        public TypeReference? CatchType {
            get; set;
        }
    }
}
