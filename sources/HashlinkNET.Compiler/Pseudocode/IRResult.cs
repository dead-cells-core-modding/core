
using HashlinkNET.Compiler.Pseudocode.IR;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode
{
    class IRResult
    {
        public int Index
        {
            get; set;
        }
        public bool IsNone => IR is null;
        public IRBase? IR
        {
            get; set;
        }

        public TypeReference? Emit( EmitContext ctx, bool requestValue = false )
        {
            if (IR == null && requestValue)
            {
                throw new InvalidOperationException();
            }
            return IR?.Emit(ctx, requestValue);
        }

        public static implicit operator IRResult( IRBase? ir )
        {
            return new()
            {
                IR = ir
            };
        }
    }
}
