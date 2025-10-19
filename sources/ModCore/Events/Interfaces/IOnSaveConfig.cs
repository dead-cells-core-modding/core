namespace ModCore.Events.Interfaces
{
    /// <summary>
    /// An event is triggered when the configuration is saved.
    /// </summary>
    [Event]
    public interface IOnSaveConfig
    {
        /// <summary>
        /// 
        /// </summary>
        public void OnSaveConfig();
    }
}
