using ModCore.Events.Interfaces;

namespace ModCore.Plugins
{
    public abstract class PluginBase : Module, IOnPluginInitializing
    {
        protected abstract void Initialize();
        void IOnPluginInitializing.OnPluginInitializing()
        {
            Logger.Information("Initializing");
            Initialize();
        }
    }
}
