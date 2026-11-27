using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// An event raised after a pak file has been loaded.
    /// </summary>
    [Event]
    public interface IOnLoadedPak
    {
        /// <summary>
        /// Raised after a pak file has been loaded.
        /// </summary>
        /// <param name="path">The path of the loaded pak file.</param>
        public void OnLoadedPak( string path );
    }
}
