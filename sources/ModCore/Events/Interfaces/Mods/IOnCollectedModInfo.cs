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
        /// 
        /// </summary>
        /// <param name="info"></param>
        void OnCollectedModInfo(ModInfo info);
    }
}
