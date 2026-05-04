using dc.ui;

namespace ModCore.Menu
{
    /// <summary>
    /// Defines the contract for a mod menu, providing methods to retrieve the menu's name, optional subtext, and to
    /// construct the menu with specified options.
    /// </summary>
    /// <remarks>Implementations of this interface should provide the logic for displaying the menu and
    /// handling user interactions based on the provided options. The interface allows for customization of menu
    /// appearance and behavior by supplying different option sets.</remarks>
    public interface IModMenu
    {
        /// <summary>
        /// Retrieves a substring from the current text context.
        /// </summary>
        /// <returns>A string containing the extracted substring, or null if no substring is available.</returns>
        public string? GetSubText() => null;
        /// <summary>
        /// Gets the name associated with the current instance.
        /// </summary>
        /// <returns>A string representing the name. Returns an empty string if no name is set.</returns>
        public string GetName();
        /// <summary>
        /// Configures and constructs the menu based on the specified options.
        /// </summary>
        /// <remarks>This method updates the menu structure according to the provided options. Ensure that
        /// the options parameter contains valid values before calling this method.</remarks>
        /// <param name="options">The options that define the configuration and appearance of the menu. Cannot be null.</param>
        public void BuildMenu( Options options );
    }
}
