namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// An event is triggered when the game attempts to exit.
    /// </summary>
    [Event(true)]
    public interface IOnGameExit
    {
        /// <summary>
        /// Raised when the game is about to exit.
        /// </summary>
        public void OnGameExit();
    }
}
