using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Mods;
using ModCore.Events.Interfaces.VM;
using ModCore.Mods;
using ModCore.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    internal class ModLoader : CoreModule<ModLoader>,
        IOnCoreModuleInitializing
    {
        public const string MODINFO_NAME = "modinfo.json";
        public override int Priority => ModulePriorities.ModLoader;
        public readonly List<ModInfo> modInfos = [];

        void IOnCoreModuleInitializing.OnCoreModuleInitializing()
        {
            Logger.Information("Registering mods type");
            Dictionary<string, Type> modsType = [];
            EventSystem.BroadcastEvent<IOnRegisterModsType, IOnRegisterModsType.AddModType>((type, info) =>
            {
                Logger.Information("Registered mod type: {type} -> {info}", type, info.FullName);
                modsType.Add(type.ToLower(), info);
            });
            Logger.Information("Collecting mods information");
            foreach(var dir in FolderInfo.Mods.Info.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                var p = Path.Combine(dir.FullName, MODINFO_NAME);
                if(!File.Exists(p))
                {
                    continue;
                }
                Logger.Information("Trying to collect information from {path}", p);
                try
                {
                    JObject jinfo = JObject.Parse(File.ReadAllText(p));
                    var name = jinfo["name"]!.ToString();
                    Logger.Information("Collect mod info: {name} {version}", name, jinfo["version"]);

                    var type = jinfo["type"]!.ToString().ToLower();
                    if(!modsType.TryGetValue(type, out var infotype))
                    {
                        Logger.Error("Unknown mod type: {type}", type);
                        continue;
                    }
                    var info = (ModInfo?)jinfo.ToObject(infotype);
                    if(info == null)
                    {
                        Logger.Error("Unable to create mod info object", type);
                        continue;
                    }
                    info.ModRoot = new("ModRoot_" + name, dir.FullName);
                    EventSystem.BroadcastEvent<IOnCollectedModInfo, ModInfo>(info);
                    modInfos.Add(info);
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
