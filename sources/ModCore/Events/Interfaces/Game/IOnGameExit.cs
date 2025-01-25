namespace ModCore.Events.Interfaces.Game
{
    [Event(true)]
    public interface IOnGameExit
    {
        public void OnGameExit();
    }
}
