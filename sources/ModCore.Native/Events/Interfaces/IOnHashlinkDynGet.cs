using Hashlink;
using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Native.Events.Interfaces
{
    [Event]
    internal interface IOnHashlinkDynGet
    {
        public record struct Data(nint ptr, int hfield, nint ptype);
        public EventResult<object?> OnHashlinkDynGet(Data data);
    }
}
