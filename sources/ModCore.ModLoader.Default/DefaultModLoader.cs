using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Mods;
using ModCore.Events.Interfaces.VM;
using ModCore.Mods;
using ModCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.ModLoader.Default
{
    [Plugin()]
    internal class DefaultModLoader : Module<DefaultModLoader>,
        IOnRegisterModsType,
        IOnCollectedModInfo,
        IOnBeforeGameInit
    {
        private readonly List<DefaultModInfo> mods = [];
        void IOnCollectedModInfo.OnCollectedModInfo( ModInfo info )
        {
            if (info is not DefaultModInfo dinfo)
            {
                return;
            }
            mods.Add(dinfo);
            try
            {
                foreach (var a in dinfo.Assemblies)
                {
                    var path = dinfo.ModRoot!.GetFilePath(a);
                    Logger.Information("Loading assembly from {path}", path);
                    dinfo.LoadedAssemblies.Add(Assembly.LoadFrom(path));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to load mod assembly");
            }
        }

        void IOnBeforeGameInit.OnBeforeGameInit()
        {

            foreach (var v in mods)
            {
                try
                {
                    if (v.LoadedAssemblies.Count == 0)
                    {
                        continue;
                    }
                    Logger.Information("Loading mod: {name} {version}", v.Name, v.Version);
                    var mt = v.MainModType;
                    if (string.IsNullOrEmpty(mt))
                    {
                        Logger.Information("Loaded as library");
                        continue;
                    }
                    Type? type = null;
                    foreach (var a in v.LoadedAssemblies)
                    {
                        type = a.GetType(mt, false, true);
                        if (type != null)
                        {
                            break;
                        }
                    }
                    if (type == null)
                    {
                        throw new MissingMemberException(mt, (string?)null);
                    }
                    if (!type.IsSubclassOf(typeof(ModBase)))
                    {
                        throw new InvalidOperationException("Main type does not inherit from ModBase");
                    }
                    Logger.Information("Constructing mod instance");
                    var mod = (ModBase)Activator.CreateInstance(type, v)!;
                    mod.Initialize();
                    Logger.Information("Loading mod completed");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unable to load mod");
                }
            }
        }

        void IOnRegisterModsType.OnRegisterModsType( IOnRegisterModsType.AddModType add )
        {
            add("mod", typeof(DefaultModInfo));
            add("library", typeof(DefaultModInfo));
            add("default", typeof(DefaultModInfo));
        }
    }
}
