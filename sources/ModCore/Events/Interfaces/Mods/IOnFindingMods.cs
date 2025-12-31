using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Events.Interfaces.Mods
{
    /// <summary>
    /// 
    /// </summary>
    [Event(true)]
    public interface IOnFindingMods
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="findMod"></param>
        public void OnFindingMods(Action<string> findMod);
    }
}
