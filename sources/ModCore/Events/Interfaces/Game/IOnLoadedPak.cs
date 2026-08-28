using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// 
    /// </summary>
    [Event]
    public interface IOnLoadedPak
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public void OnLoadedPak( string path );
    }
}
