namespace ModCore.Events.Interfaces
{
    /// <summary>
    /// An event is triggered when the configuration is saved.
    /// </summary>
    [Event]
    public interface IOnSaveConfig
    {
        /// <summary>
        /// Raised when the configuration should be saved.
        /// </summary>
        public void OnSaveConfig();
    }
}
