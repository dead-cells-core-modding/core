namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// An event is triggered when the game starts initialization
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="IOnBeforeGameInit"/>, this event is actually triggered when the window is created.
    /// </remarks>
    [Event(true)]
    public interface IOnGameInit
    {
        /// <summary>
        /// Raised when the game starts initializing.
        /// </summary>
        public void OnGameInit();
    }
}
