using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Marshaling
{
    public unsafe class HashlinkError : Exception
    {
        public nint Error { get; }
        public HashlinkError(nint err) : base(
            $"Uncaught hashlink exception.{(err == 0 ? "<null>" : hl_to_string((HL_vdynamic*)err))}"
            )
        {
            Error = err;
        }
    }
}
