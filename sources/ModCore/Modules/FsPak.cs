using dc;
using dc.tool.mod;
using dc.ui;
using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    /// <summary>
    /// 
    /// </summary>
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    public class FsPak : CoreModule<FsPak>,
        IOnBeforeGameInit
    {
        /// <inheritdoc/>
        public override int Priority => ModulePriorities.Game;
        /// <summary>
        /// Get the game's pak loader
        /// </summary>
        public dc.hxd.fmt.pak.FileSystem FileSystem { get; private set; } = null!;

        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            dc.hxd.fmt.pak.Hook_Reader.readHeader += Hook_Reader_readHeader1;
            dc.hxd.fmt.pak.Hook_FileSystem.loadPak += Hook_FileSystem_loadPak1;
            Hook__Assets.init += Hook__Assets_init;
        }

        private bool Hook__Assets_init( Hook__Assets.orig_init orig )
        {
            var result = orig();
            EventSystem.BroadcastEvent<IOnAfterLoadingAssets>();
            Data.Class.loadJson(CDBManager.Class.instance.getAlteredCDB(), default);
            return result;
        }

        private void Hook_FileSystem_loadPak1( dc.hxd.fmt.pak.Hook_FileSystem.orig_loadPak orig, 
            dc.hxd.fmt.pak.FileSystem self, dc.String file )
        {
            if (FileSystem == null)
            {
                FileSystem = self;
            }
            Logger.Information("Loading pak from {path}", file.ToString());
            orig(self, file);
        }

        private dc.hxd.fmt.pak.Data Hook_Reader_readHeader1( dc.hxd.fmt.pak.Hook_Reader.orig_readHeader orig, dc.hxd.fmt.pak.Reader self )
        {
            dc.hxd.fmt.pak.FileSystem.Class.PAK_STAMP_HASH = null;
            var data = orig(self);
            data.stampHash = null;
            return data;
        }
    }
}
