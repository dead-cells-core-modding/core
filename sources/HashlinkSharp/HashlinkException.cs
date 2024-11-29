using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink
{
    public unsafe class HashlinkException : Exception
    {
        public HL_vdynamic* error;
        public HashlinkException() : this("The Hashlink VM raised an exception.") { }
        public HashlinkException(HL_vdynamic* val) : this("The Hashlink VM raised an exception: " + 
            HashlinkNative.hl_to_string(val))
        {
            error = val;
        }
        public HashlinkException(string message) : base(message) { }
        public HashlinkException(string message, Exception inner) : base(message, inner) { }
    }
}
