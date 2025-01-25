namespace ModCore.Events.Interfaces
{
    [Event(true)]
    public interface IOnPluginInitialized
    {
        public void OnPluginInitialized();
    }
}
