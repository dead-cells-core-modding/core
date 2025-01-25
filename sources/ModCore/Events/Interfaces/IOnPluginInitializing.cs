namespace ModCore.Events.Interfaces
{
    [Event(true)]
    public interface IOnPluginInitializing
    {
        public void OnPluginInitializing();
    }
}
