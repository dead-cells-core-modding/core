using Hashlink.Reflection.Types;
using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace HaxeProxy.Events
{
    [Event]
    internal interface IOnHashlinkCreateEmptyInstance
    {
        public EventResult<object> OnHashlinkCreateEmptyInstance( HashlinkType type );
    }
}
