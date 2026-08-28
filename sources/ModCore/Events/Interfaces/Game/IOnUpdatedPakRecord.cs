using dc.hxd.fmt.pak;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// 
    /// </summary>
    [Event]
    public interface IOnUpdatedPakRecord
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public void OnUpdatedPakRecord( string path );
    }
}
