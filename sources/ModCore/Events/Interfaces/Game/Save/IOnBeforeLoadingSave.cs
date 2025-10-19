using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game.Save
{
    /// <summary>
    /// An event is triggered before loading a save file.
    /// </summary>
    [Event]
    public interface IOnBeforeLoadingSave
    {
        /// <summary>
        /// 
        /// </summary>
        void OnBeforeLoadingSave();
    }
}
