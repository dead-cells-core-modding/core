using dc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game.Save
{
    /// <summary>
    ///  An event is triggered before saving a save file.
    /// </summary>
    [Event]
    public interface IOnBeforeSavingSave
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Data">Save data</param>
        /// <param name="OnlyGameData">Whether to include only game data</param>
        public record class EventData(User Data, bool OnlyGameData);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        void OnBeforeSavingSave( EventData data );
    }
}
