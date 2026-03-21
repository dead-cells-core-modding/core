using dc;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore
{
    /// <summary>
    /// 
    /// </summary>
    public static class GameInfo
    {
        /// <summary>
        /// Gets the current version number of the game.
        /// </summary>
        public static int GameVersion => Main.Class.GAME_VERSION;

        /// <summary>
        /// Gets the version of the DCCM.
        /// </summary>
        public static Version DCCMVersion
        {
            get;
        } = typeof(GameInfo).Assembly.GetName().Version!;
    }
}
