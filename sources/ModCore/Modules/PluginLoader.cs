using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Plugins;
using ModCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    public class PluginLoader : CoreModule<PluginLoader>, IOnModCoreInjected
    {
        public override int Priority => ModulePriorities.PluginLoader;

        void IOnModCoreInjected.OnModCoreInjected()
        {
            Logger.Information("Loading plugins");
            foreach (var v in FolderInfo.Plugins.Info.GetFiles("*.dll"))
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

            Logger.Information("Initializing plugins");
            EventSystem.BroadcastEvent<IOnPluginInitializing>(
                EventSystem.ExceptionHandingFlags.Continue | EventSystem.ExceptionHandingFlags.NoThrow);
            Logger.Information("Plugins initialization completed");

        }
    }
}
