namespace ModCore.Events.Interfaces.Game
{
    [Event(true)]
    public interface IOnGameInit
    {
        public void OnGameInit();
    }
}
