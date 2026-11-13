using dc;
using HaxeProxy.Runtime;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Mods;
using ModCore.Mods;
using ModCore.Modules;
using ModCore.Plugins;
using ModCore.Utilities;

namespace ModCore.ModLoader.Default
{
    [Plugin]
    internal class SideResModLoader : PluginBase,
        IOnRegisterModsType,
        IOnCollectedModInfo,
        IOnBeforeGameInit
    {
        private readonly List<string> resPaks = [];

        protected override void Initialize()
        {

        }

        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            Hook__Boot.initRes += Hook__Boot_initRes;
        }

        private void Hook__Boot_initRes( Hook__Boot.orig_initRes orig )
        {
            orig();
            foreach (var v in resPaks)
            {
                Logger.Information("Loading mod res pak: {pak}", v);
                FsPak.Instance.FileSystem.loadPak(v.AsHaxeString());
            }
            CDBManager.Instance.LoadJsonData(CDBManager.Instance.GetAlteredCDB());
        }

        void IOnCollectedModInfo.OnCollectedModInfo( ModInfo info )
        {
            if (info is not SideResModInfo ri)
            {
                return;
            }
            foreach (var v in ri.Paks)
            {
                resPaks.Add(v);
            }
            _  = new SideResMod(info);
        }

        void IOnRegisterModsType.OnRegisterModsType( IOnRegisterModsType.AddModType add )
        {
            add("res", typeof(SideResModInfo));
        }
    }
}
