using HashlinkNET.Compiler.Pseudocode.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace HashlinkNET.Compiler.Pseudocode
{
    abstract class IRBaseWithSideEffects(params IRResult?[] values) : IRBase(values)
    {
        public override bool HasSideEffects => true;
    }
}
