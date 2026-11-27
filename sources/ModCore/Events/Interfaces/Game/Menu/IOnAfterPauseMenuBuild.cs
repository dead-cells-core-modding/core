using dc.ui;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Events.Interfaces.Game.Menu
{
    /// <summary>
    /// An event raised after the pause menu has been built.
    /// </summary>
    [Event]
    public interface IOnAfterPauseMenuBuild
    {
        /// <summary>
        /// Raised after the pause menu has been built.
        /// </summary>
        /// <param name="pause">The pause menu that was built.</param>
        public void OnAfterPauseMenuBuild( Pause pause );
    }
}
