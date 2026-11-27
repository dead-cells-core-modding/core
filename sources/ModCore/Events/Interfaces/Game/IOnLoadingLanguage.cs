namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// An event raised when a language is being loaded.
    /// </summary>
    [Event()]
    public interface IOnLoadingLanguage
    {
        /// <summary>
        /// Raised when a language is being loaded.
        /// </summary>
        /// <param name="lang">The identifier of the language being loaded.</param>
        public void OnLoadingLanguage( string lang );
    }
}
