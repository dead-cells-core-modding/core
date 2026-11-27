using dc.hxd.fmt.pak;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// An event raised after a pak record has been updated.
    /// </summary>
    [Event]
    public interface IOnUpdatedPakRecord
    {
        /// <summary>
        /// Raised after a pak record has been updated.
        /// </summary>
        /// <param name="path">The path of the updated pak record.</param>
        public void OnUpdatedPakRecord( string path );
    }
}
