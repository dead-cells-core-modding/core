namespace ModCore.Events.Interfaces
{
    /// <summary>
    /// An event triggered when the game resources are loaded.
    /// </summary>
    /// <remarks>
    /// You can load your custom res.pak here
    /// </remarks>
    [Event]
    public interface IOnAfterLoadingAssets
    {
        /// <summary>
        /// Raised after the game assets have been loaded.
        /// </summary>
        void OnAfterLoadingAssets();
    }
}
