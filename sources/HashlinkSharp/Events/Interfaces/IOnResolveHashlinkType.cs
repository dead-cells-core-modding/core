using Hashlink.Reflection.Types;
using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hashlink.Events.Interfaces
{
    [Event]
    internal interface IOnResolveHashlinkType
    {
        EventResult<HashlinkType> OnResolveHashlinkType( Type type );
    }
}
