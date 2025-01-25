namespace ModCore.Events.Interfaces
{
    [Event(true)]
    public interface IOnCoreModuleInitializing
    {
        void OnCoreModuleInitializing();
    }
}
