using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class EventAttribute(bool once = false) : Attribute
    {
        public bool Once { get; } = once;
    }
}
