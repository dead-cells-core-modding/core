namespace ModCore.Events.Interfaces
{
    /// <summary>
    /// An event is triggered when all plugins are initialized.
    /// </summary>
    [Event(true)]
    public interface IOnPluginInitialized
    {
        /// <summary>
        /// 
        /// </summary>
        public void OnPluginInitialized();
    }
}
