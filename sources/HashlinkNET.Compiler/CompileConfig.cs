using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler
{
    public class CompileConfig
    {
        public bool AllowParalle
        {
            get; set;
        }
        public bool GeneratePseudocode
        {
            get; set;
        }
        public bool GenerateBytecodeMapping
        {
            get; set;
        }
    }
}
