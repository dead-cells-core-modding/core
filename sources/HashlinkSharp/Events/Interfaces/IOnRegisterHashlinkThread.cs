using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Events.Interfaces
{
    [Event]
    public interface IOnRegisterHashlinkThread
    {
        void OnRegisterHashlinkThread();
    }
}
