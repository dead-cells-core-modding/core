using dc;

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
