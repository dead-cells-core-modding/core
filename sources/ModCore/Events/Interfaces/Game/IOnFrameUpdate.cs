namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    /// An event is triggered every frame
    /// </summary>
    [Event]
    public interface IOnFrameUpdate
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        public void OnFrameUpdate( double dt );
    }
}
