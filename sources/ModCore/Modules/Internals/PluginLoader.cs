using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Plugins;
using ModCore.Storage;
using System.Reflection;
using System.Text;

namespace ModCore.Modules.Internals
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    internal class PluginLoader : CoreModule<PluginLoader>, IOnCoreModuleInitializing
    {
        private readonly CacheFile lastLoadedExtraPlugins = new("last_load_extra_plugins_paths");
        public override int Priority => ModulePriorities.PluginLoader;

        void IOnCoreModuleInitializing.OnCoreModuleInitializing()
        {
            Logger.Information("Loading plugins");

            List<string> plugins = [.. FolderInfo.Plugins.Info.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Select(x => x.FullName)];

            var pluginsPathStr = Environment.GetEnvironmentVariable("DCCM_EXTRA_PLUGINS_PATHS");

            if (pluginsPathStr == null && lastLoadedExtraPlugins.TryGetCache(out var modsPathStrBytes))
            {
                pluginsPathStr = Encoding.UTF8.GetString(modsPathStrBytes);
            }

            if (!string.IsNullOrWhiteSpace(pluginsPathStr))
            {
                plugins.AddRange(
                    pluginsPathStr.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Path.GetFullPath)
                    );
            }

            lastLoadedExtraPlugins.UpdateCache(Encoding.UTF8.GetBytes(pluginsPathStr ?? ""));


            foreach (var dir in plugins)
            {
                foreach (var v in Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var fi = new FileInfo(v);
                        Logger.Information("Loading {path}", fi.Name);
                        var asm = Assembly.LoadFrom(fi.FullName);
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
