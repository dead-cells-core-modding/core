using Hashlink.Proxy;
using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haxe.Event.Interfaces
{
    [Event]
    public interface IOnConvertHashlinkObject
    {
        EventResult<HaxeObjectBase> OnConvertHashlinkObject( HashlinkObj obj );
    }
}
