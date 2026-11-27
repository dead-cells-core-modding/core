namespace ModCore.Events.Interfaces
{
    /// <summary>
    /// An event is triggered when the plugin is initializing
    /// </summary>
    [Event(true)]
    public interface IOnPluginInitializing
    {
        /// <summary>
        /// Raised when plugins are being initialized.
        /// </summary>
        public void OnPluginInitializing();
    }
}
