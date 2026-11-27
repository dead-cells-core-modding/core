namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// An event is triggered every frame
    /// </summary>
    [Event]
    public interface IOnFrameUpdate
    {
        /// <summary>
        /// Raised once per frame.
        /// </summary>
        /// <param name="dt">The elapsed time since the previous frame, in seconds.</param>
        public void OnFrameUpdate( double dt );
    }
}
