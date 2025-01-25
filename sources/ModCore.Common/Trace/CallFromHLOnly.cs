using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Trace
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CallFromHLOnly : Attribute
    {
    }
}
