namespace ModCore.Menu
{
    /// <summary>
    /// Defines a contract for providing a mod menu instance associated with the implementing class.
    /// </summary>
    /// <remarks>Implementations of this interface should return the specific mod menu relevant to their
    /// context. This enables dynamic retrieval of mod menus based on the application's current state or
    /// configuration.</remarks>
    public interface IModMenuProvider
    {
        /// <summary>
        /// Retrieves the mod menu interface used to manage game modifications.
        /// </summary>
        /// <returns>An instance of an object that implements the <see cref="IModMenu"/> interface, providing access to mod
        /// management features.</returns>
        public IModMenu GetModMenu();
    }
}
