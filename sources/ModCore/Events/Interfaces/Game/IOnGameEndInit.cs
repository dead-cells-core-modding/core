namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// An event is triggered when the game is initialized.
    /// </summary>
    [Event(true)]
    public interface IOnGameEndInit
    {
        /// <summary>
        /// 
        /// </summary>
        public void OnGameEndInit();
    }
}
