using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Mods;
using ModCore.Mods;
using ModCore.Storage;
using Newtonsoft.Json.Linq;
using System.Text;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    internal class ModLoader : CoreModule<ModLoader>,
        IOnCoreModuleInitializing
    {
        private readonly CacheFile lastLoadedExtraMods = new("last_load_extra_mods_paths");
        public const string MODINFO_NAME = "modinfo.json";
        public override int Priority => ModulePriorities.ModLoader;
        public readonly Dictionary<string, ModInfo> modInfos = [];

        void IOnCoreModuleInitializing.OnCoreModuleInitializing()
        {
            Logger.Information("Registering mods type");
            Dictionary<string, Type> modsType = [];
            EventSystem.BroadcastEvent<IOnRegisterModsType, IOnRegisterModsType.AddModType>(( type, info ) =>
            {
                Logger.Information("Registered mod type: {type} -> {info}", type, info.FullName);
                modsType.Add(type.ToLowerInvariant(), info);
            });
            Logger.Information("Collecting mods information");

            List<string> mods = [.. FolderInfo.Mods.Info.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Select(x => x.FullName)];

            var modsPathStr = Environment.GetEnvironmentVariable("DCCM_EXTRA_MODS_PATHS");

            if (modsPathStr == null && lastLoadedExtraMods.TryGetCache(out var modsPathStrBytes))
            {
                modsPathStr = Encoding.UTF8.GetString(modsPathStrBytes);
            }

            if (!string.IsNullOrWhiteSpace(modsPathStr))
            {
                mods.AddRange(
                    modsPathStr.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Path.GetFullPath)
                    );
            }

            lastLoadedExtraMods.UpdateCache(Encoding.UTF8.GetBytes(modsPathStr ?? ""));


            EventSystem.BroadcastEvent<IOnFindingMods, Action<string>>(path =>
            {
                path = Path.GetFullPath(path);
                mods.Add(path);
            });

            foreach (var dir in mods)
            {
                var p = Path.Combine(dir, MODINFO_NAME);
                if (!File.Exists(p))
                {
                    continue;
                }
                Logger.Information("Trying to collect information from {path}", p);
                try
                {
                    JObject jinfo = JObject.Parse(File.ReadAllText(p));
                    var name = jinfo["name"]!.ToString();
                    if (modInfos.ContainsKey(name))
                    {
                        Logger.Warning("Mod with name {name} already collected, skipping", name);
                        continue;
                    }
                    Logger.Information("Collect mod info: {name} {version}", name, jinfo["version"]?.ToString());

                    var type = jinfo["type"]!.ToString().ToLowerInvariant();
                    if (!modsType.TryGetValue(type, out var infotype))
                    {
                        Logger.Error("Unknown mod type: {type}", type);
                        continue;
                    }
                    var info = (ModInfo?)jinfo.ToObject(infotype);
                    if (info == null)
                    {
                        Logger.Error("Unable to create mod info object", type);
                        continue;
                    }
                    if (!string.IsNullOrEmpty(info.DCCMVersion))
                    {
                        var buildDCCMVer = Version.Parse(info.DCCMVersion);
                        if (buildDCCMVer > GameInfo.DCCMVersion ||
                                buildDCCMVer.Major != GameInfo.DCCMVersion.Major)
                        {
                            Logger.Warning("The target DCCM version for {A} is {B}, which does not match the current version.",
                                info.Name, info.DCCMVersion);
                        }
                    }
                    info.ModRoot = new("ModRoot_" + name, dir);
                    EventSystem.BroadcastEvent<IOnCollectedModInfo, ModInfo>(info);
                    modInfos.Add(name, info);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unable to collect information");
                }
            }
            Logger.Information("Mods information collection completed");
        }
    }
}
