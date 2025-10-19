using ModCore.Events.Interfaces;

namespace ModCore.Plugins
{
    /// <summary>
    /// Base class for all plugins. 
    /// For a plugin to be loaded by the plugin loader, the <see cref="PluginAttribute"/> attribute is also required
    /// </summary>
    public abstract class PluginBase : Module, IOnPluginInitializing
    {
        /// <summary>
        /// Called when the plugin is being initialized
        /// </summary>
        protected abstract void Initialize();
        void IOnPluginInitializing.OnPluginInitializing()
        {
            Logger.Information("Initializing");
            Initialize();
        }
    }

}
