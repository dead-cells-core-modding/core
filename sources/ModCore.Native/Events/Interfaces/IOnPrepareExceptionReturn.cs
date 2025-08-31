using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Native.Events.Interfaces
{
    [Event]
    internal interface IOnPrepareExceptionReturn
    {
        public EventResult<nint> OnPrepareExceptionReturn(nint data);
    }
}
