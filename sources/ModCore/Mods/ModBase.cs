namespace ModCore.Mods
{
    /// <summary>
    /// Base class for all mods
    /// </summary>
    /// <param name="info"></param>
    public class ModBase( ModInfo info ) : Module
    {
        /// <summary>
        /// Metadata about the mod
        /// </summary>
        public ModInfo Info { get; } = info;

        /// <summary>
        /// Called when the mod is being initialized
        /// </summary>
        public virtual void Initialize()
        {

        }

        /// <summary>
        /// Gets the display name associated with the mod.
        /// </summary>
        /// <returns>A string representing the display name, which is derived from the Info.Name property.</returns>
        public virtual string GetDisplayName()
        {
            return Info.Name;
        }
    }
}
