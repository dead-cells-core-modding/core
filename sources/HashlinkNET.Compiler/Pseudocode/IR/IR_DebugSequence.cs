using HashlinkNET.Bytecode;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    internal class IR_DebugSequence(HlFunDebug debug) : IRBase
    {
        public HlFunDebug Info => debug;
        protected override TypeReference? Emit( EmitContext ctx, 
            IDataContainer container, 
            ILProcessor il )
        {
            var inst = Instruction.Create(OpCodes.Nop);
            inst.Operand = this;
            il.Append( inst );
            return null;
        }
    }
}
