using dc;
using dc.tool.mod;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Storage;
using ModCore.Utilities;
using ModCore.Utitities;

namespace ModCore.Modules
{
    /// <summary>
    /// 
    /// </summary>
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    public class FsPak : CoreModule<FsPak>,
        IOnBeforeGameInit,
        IOnGameInit
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
            Hook__Boot.initRes += Hook__Boot_initRes;
        }

        private void Hook__Boot_initRes( Hook__Boot.orig_initRes orig )
        {
            orig();
            EventSystem.BroadcastEvent<IOnAfterLoadingAssets>();
        }

        private bool Hook__Assets_init( Hook__Assets.orig_init orig )
        {
            var result = orig();
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

        void IOnGameInit.OnGameInit()
        {
            FileSystem.loadPak(FolderInfo.CoreRoot.GetFilePath("core/host/res.pak").AsHaxeString());
        }
    }
}
