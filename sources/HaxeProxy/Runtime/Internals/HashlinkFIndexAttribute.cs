using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HashlinkFIndexAttribute(int findex) : Attribute
    {

        public int Index
        {
            get;
        } = findex;
    }
}
