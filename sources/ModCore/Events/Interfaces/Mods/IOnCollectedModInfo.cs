using ModCore.Mods;

namespace ModCore.Events.Interfaces.Mods
{
    /// <summary>
    /// An event that is triggered when the mods loader processes a mod
    /// </summary>
    [Event]
    public interface IOnCollectedModInfo
    {
        /// <summary>
        /// Raised when the mod loader collects a mod.
        /// </summary>
        /// <param name="info">The metadata of the collected mod.</param>
        void OnCollectedModInfo( ModInfo info );
    }
}
