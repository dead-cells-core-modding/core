using dc;

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
