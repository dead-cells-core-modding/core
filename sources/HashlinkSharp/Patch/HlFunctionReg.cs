using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Patch
{
    public class HlFunctionReg(HashlinkType type)
    {
        public override string ToString()
        {
            return $"[{Index}: {Type}]";
        }
        public HashlinkType Type
        {
            get; set;
        } = type;
        internal int Index
        {
            get; set;
        }
    }
}
