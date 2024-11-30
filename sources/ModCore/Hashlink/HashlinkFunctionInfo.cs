using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hashlink
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HashlinkFunctionInfo(string filename, int startLine, string endFileName, int endLine) : Attribute
    {

    }
}
