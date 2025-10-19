using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game.Hero
{
    /// <summary>
    /// An event is triggered every frame when the hero exists.
    /// </summary>
    [Event]
    public interface IOnHeroUpdate
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        void OnHeroUpdate( double dt );
    }
}
