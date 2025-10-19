namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// An event is triggered when the game attempts to exit.
    /// </summary>
    [Event(true)]
    public interface IOnGameExit
    {
        /// <summary>
        /// 
        /// </summary>
        public void OnGameExit();
    }
}
