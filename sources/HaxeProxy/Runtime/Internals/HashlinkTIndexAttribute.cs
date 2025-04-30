using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
        AttributeTargets.Delegate)]
    public class HashlinkTIndexAttribute( int tindex ) : Attribute
    {

        public int Index
        {
            get;
        } = tindex;
    }
}
