using dc.ui;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Events.Interfaces.Game.Menu
{
    /// <summary>
    /// 
    /// </summary>
    [Event]
    public interface IOnAfterPauseMenuBuild
    {
        /// <summary>
        /// 
        /// </summary>
        public void OnAfterPauseMenuBuild( Pause pause );
    }
}
