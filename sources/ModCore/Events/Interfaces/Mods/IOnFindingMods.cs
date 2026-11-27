namespace ModCore.Events.Interfaces.Mods
{
    /// <summary>
    /// An event raised when the mod loader searches for mods.
    /// </summary>
    [Event(true)]
    public interface IOnFindingMods
    {
        /// <summary>
        /// Raised when the mod loader searches for mods.
        /// </summary>
        /// <param name="findMod">A callback that registers a mod by its path.</param>
        public void OnFindingMods( Action<string> findMod );
    }
}
