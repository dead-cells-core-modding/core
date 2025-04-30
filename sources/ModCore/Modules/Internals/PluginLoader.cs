using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Plugins;
using ModCore.Storage;
using System.Reflection;

namespace ModCore.Modules.Internals
{
    [CoreModule]
    internal class PluginLoader : CoreModule<PluginLoader>, IOnCoreModuleInitializing
    {
        public override int Priority => ModulePriorities.PluginLoader;

        void IOnCoreModuleInitializing.OnCoreModuleInitializing()
        {
            Logger.Information("Loading plugins");
            foreach (var dir in FolderInfo.Plugins.Info.GetDirectories("*"))
            {
                foreach (var v in dir.GetFiles("*.dll"))
                {
                    try
                    {
                        Logger.Information("Loading {path}", v.Name);
                        var asm = Assembly.LoadFrom(v.FullName);
                        Logger.Information("Finding plugins");
                        foreach (var t in asm.SafeGetAllTypes())
                        {
                            if (t?.IsAbstract ?? true)
                            {
                                continue;
                            }
                            if (!t.IsSubclassOf(typeof(Module)))
                            {
                                continue;
                            }
                            var attr = t.GetCustomAttribute<PluginAttribute>();
                            if (attr == null)
                            {
                                continue;
                            }
                            Logger.Information("Creating a new instance: {type}", t.FullName);
                            Activator.CreateInstance(t);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "An exception occurred when loading plugin");
                    }
                }
            }

            Logger.Information("Initializing plugins");
            EventSystem.BroadcastEvent<IOnPluginInitializing>(
                EventSystem.ExceptionHandingFlags.Continue | EventSystem.ExceptionHandingFlags.NoThrow);
            Logger.Information("Plugins initialization completed");

        }
    }
}
