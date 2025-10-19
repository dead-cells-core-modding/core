using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game.Hero
{
    /// <summary>
    /// An event is triggered when the hero is being initialized.
    /// </summary>
    [Event]
    public interface IOnHeroInit
    {
        /// <summary>
        /// 
        /// </summary>
        void OnHeroInit();
    }
}
