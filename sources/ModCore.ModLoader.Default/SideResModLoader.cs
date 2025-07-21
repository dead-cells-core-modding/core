using dc;
using dc.tool.mod;
using HaxeProxy.Runtime;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Mods;
using ModCore.Mods;
using ModCore.Modules;
using ModCore.Plugins;
using ModCore.Utitities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.ModLoader.Default
{
    [Plugin]
    internal class SideResModLoader : PluginBase ,
        IOnRegisterModsType,
        IOnCollectedModInfo,
        IOnBeforeGameInit
    {
        private readonly List<string> resPaks = [];
        private HlAction oldInitRes = null!;
        protected override void Initialize()
        {

        }

        private void Hook_InitRes()
        {
            oldInitRes();
            foreach (var v in resPaks)
            {
                Logger.Information("Loading mod res pak: {pak}", v);
                FsPak.Instance.FileSystem.loadPak(v.AsHaxeString());
            }
            Data.Class.loadJson(CDBManager.Class.instance.getAlteredCDB(), default);
        }

        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            oldInitRes = dc.Boot.Class.initRes;
            dc.Boot.Class.initRes = Hook_InitRes;
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
        }

        void IOnRegisterModsType.OnRegisterModsType( IOnRegisterModsType.AddModType add )
        {
            add("res", typeof(SideResModInfo));
        }
    }
}
