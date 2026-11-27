namespace ModCore.Events.Interfaces
{
    /// <summary>
    /// An event is triggered when all plugins are initialized.
    /// </summary>
    [Event(true)]
    public interface IOnPluginInitialized
    {
        /// <summary>
        /// Raised after all plugins have been initialized.
        /// </summary>
        public void OnPluginInitialized();
    }
}
