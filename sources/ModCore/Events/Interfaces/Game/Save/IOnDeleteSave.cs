using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game.Save
{
    /// <summary>
    /// An event triggered when deleting a save
    /// </summary>
    [Event]
    public interface IOnDeleteSave
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="slot"></param>
        void OnDeleteSave( int? slot );
    }
}
