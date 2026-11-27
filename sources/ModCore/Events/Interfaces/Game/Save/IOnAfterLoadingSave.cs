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
        /// Raised after a save file has been loaded.
        /// </summary>
        /// <param name="data">The loaded user save data.</param>
        void OnAfterLoadingSave( User data );
    }
}
