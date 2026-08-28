using dc;
using dc.hxd.fmt.pak;
using dc.libs.heaps.slib;
using Hashlink.Proxy.Clousre;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Storage;
using ModCore.Utilities;
using System.Diagnostics;

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
            dc.hxd.fmt.pak.Hook_FileSystem.addRec += Hook_FileSystem_addRec;
            Hook__Assets.init += Hook__Assets_init;
            Hook__Boot.initRes += Hook__Boot_initRes;

            if (GameInfo.Platform == GameInfo.PlatformKind.Steam)
            {
                // Remove mod limit
                HashlinkHooks.Instance.CreateHook("hxd.fmt.pak.FileSystem", "loadModPak",
                    ( HashlinkClosure orig, dc.hxd.fmt.pak.FileSystem self,  dc.String file ) =>
                    {
                        self.loadPak(file);
                    }, true);
            }
        }

        private void Hook_FileSystem_addRec( dc.hxd.fmt.pak.Hook_FileSystem.orig_addRec orig, dc.hxd.fmt.pak.FileSystem self, 
            dc.hxd.fmt.pak.PakEntry parent, dc.String path, dc.hxd.fmt.pak.File f, 
            dc.sys.io.FileInput pak, int delta, bool modPak )
        {
            orig(self, parent, path, f, pak, delta, modPak);
            EventSystem.BroadcastEvent<IOnUpdatedPakRecord, string>(path.ToString());
        }

        private void Hook__Boot_initRes( Hook__Boot.orig_initRes orig )
        {
            orig();
            EventSystem.BroadcastEvent<IOnAfterLoadingAssets>();
        }

        private bool Hook__Assets_init( Hook__Assets.orig_init orig )
        {
            var result = orig();
            CDBManager.Instance.LoadJsonData(CDBManager.Instance.GetAlteredCDB());
            return result;
        }

        private void Hook_FileSystem_loadPak1( dc.hxd.fmt.pak.Hook_FileSystem.orig_loadPak orig,
            dc.hxd.fmt.pak.FileSystem self, dc.String file )
        {
            if (FileSystem == null)
            {
                FileSystem = self;
            }
            if(file.ToString() == "res.pak")
            {
                file = FolderInfo.GameRoot.GetFilePath("res.pak").AsHaxeString();
            }
            Logger.Information("Loading pak from {path}", file.ToString());
            orig(self, file);

            EventSystem.BroadcastEvent<IOnLoadedPak, string>(file.ToString());
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
