namespace ModCore.Events.Interfaces.Mods
{
    /// <summary>
    /// An event triggered when the mods loader searches for a valid mod type.
    /// </summary>
    [Event(true)]
    public interface IOnRegisterModsType
    {
        /// <summary>
        /// Add a new mod type to the mods loader
        /// </summary>
        /// <param name="typeName">The name of the mod type.</param>
        /// <param name="infoType">The type used to describe mod metadata.</param>
        public delegate void AddModType( string typeName, Type infoType );
        /// <summary>
        /// Raised when the mod loader searches for valid mod types.
        /// </summary>
        /// <param name="add">A callback that registers a mod type with the mod loader.</param>
        void OnRegisterModsType( AddModType add );
    }
}
