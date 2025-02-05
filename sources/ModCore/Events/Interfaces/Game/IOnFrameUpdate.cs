namespace ModCore.Events.Interfaces.Game
{
    [Event]
    public interface IOnFrameUpdate
    {
        public void OnFrameUpdate( double dt );
    }
}
