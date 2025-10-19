using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// An event fired before the game attempts to initialize
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="IOnGameInit"/>, this event is actually triggered when the main entry point of haxe is executed.
    /// </remarks>
    [Event(true)]
    public interface IOnBeforeGameInit
    {
        /// <summary>
        /// 
        /// </summary>
        void OnBeforeGameInit();
    }
}
