using ModCore.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Mods
{
    [Event]
    public interface IOnCollectedModInfo
    {
        void OnCollectedModInfo(ModInfo info);
    }
}
