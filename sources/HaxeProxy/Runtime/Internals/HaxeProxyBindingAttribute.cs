using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class HaxeProxyBindingAttribute(int typeIndex, Type type) : Attribute
    {
        public int TypeIndex => typeIndex;
        public Type Type => type;
    }
}
