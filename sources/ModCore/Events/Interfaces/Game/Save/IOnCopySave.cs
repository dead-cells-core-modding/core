using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game.Save
{
    /// <summary>
    /// An event is triggered when a save is moved
    /// </summary>
    [Event]
    public interface IOnCopySave
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SlotFrom">Original save slot ID</param>
        /// <param name="SlotTo">Destination save slot id</param>
        public record class EventData(int SlotFrom, int SlotTo);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        void OnCopySave( EventData data );
    }
}
