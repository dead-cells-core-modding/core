namespace ModCore.Events.Interfaces
{
    [Event(once: true)]
    internal interface IOnAdvancedModuleInitializing
    {
        public void OnAdvancedModuleInitializing();
    }
}
