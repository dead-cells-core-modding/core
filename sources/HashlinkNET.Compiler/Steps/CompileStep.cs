using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps
{
    internal abstract class CompileStep
    {
        public abstract void Execute(IDataContainer container);
    }
}
