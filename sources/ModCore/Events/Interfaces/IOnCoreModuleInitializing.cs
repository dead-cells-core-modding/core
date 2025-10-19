namespace ModCore.Events.Interfaces
{
    [Event(true)]
    internal interface IOnCoreModuleInitializing
    {
        void OnCoreModuleInitializing();
    }
}
