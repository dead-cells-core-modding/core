using System;
using System.Collections.Generic;
using System.Text;

namespace HaxeProxy.Runtime.Internals
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class HashlinkInitialvalueAttribute : Attribute
    {
        public HashlinkInitialvalueAttribute( object value )
        {
            _ = value;
        }
    }
}
