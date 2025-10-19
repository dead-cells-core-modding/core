using dc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game.Save
{
    /// <summary>
    /// An event triggered when a save file is loaded.
    /// </summary>
    [Event]
    public interface IOnAfterLoadingSave
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        void OnAfterLoadingSave( User data );
    }
}
