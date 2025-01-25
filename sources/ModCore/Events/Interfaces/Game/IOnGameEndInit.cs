namespace ModCore.Events.Interfaces.Game
{
    [Event(true)]
    public interface IOnGameEndInit
    {
        public void OnGameEndInit();
    }
}
