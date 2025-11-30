using ModCore.Events.Interfaces;
using ModCore.Mods;
using ModCore.Modules;
using ModCore.Storage;
using ModCore.Utitities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Assets
{
    internal class ModCoreAssetsMod( ModInfo info ) : ModBase(info),
        IOnAfterLoadingAssets
    {
        void IOnAfterLoadingAssets.OnAfterLoadingAssets()
        {
            var pakPath = Info.ModRoot!.GetFilePath("res.pak");
            FsPak.Instance.FileSystem.loadPak(pakPath.AsHaxeString());

            GetText.Instance.RegisterMod("dccm-core");
        }
    }
}
