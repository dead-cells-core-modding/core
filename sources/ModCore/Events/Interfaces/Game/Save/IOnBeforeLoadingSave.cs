namespace ModCore.Events.Interfaces.Game.Save
{
    /// <summary>
    /// An event is triggered before loading a save file.
    /// </summary>
    [Event]
    public interface IOnBeforeLoadingSave
    {
        /// <summary>
        /// Raised before a save file is loaded.
        /// </summary>
        void OnBeforeLoadingSave();
    }
}
