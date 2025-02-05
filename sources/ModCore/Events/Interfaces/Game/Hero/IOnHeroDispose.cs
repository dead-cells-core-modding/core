using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game.Hero
{
    [Event]
    public interface IOnHeroDispose
    {
        void OnHeroDispose();
    }
}
