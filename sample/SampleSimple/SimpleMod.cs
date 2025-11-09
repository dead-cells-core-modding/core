
using dc;
using dc.hxd;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Mods;
using ModCore.Modules;
using ModCore.Utitities;

namespace SampleSimple
{
    public class SimpleMod(ModInfo info) : ModBase(info),
        IOnGameExit,
        IOnGameEndInit,
        IOnAfterLoadingAssets
    {
        public override void Initialize()
        {
            Logger.Information("Hello, World!");
        }

        void IOnAfterLoadingAssets.OnAfterLoadingAssets()
        {
            var res = Info.ModRoot!.GetFilePath("res.pak");
            FsPak.Instance.FileSystem.loadPak(res.AsHaxeString());
        }

        void IOnGameEndInit.OnGameEndInit()
        {
            var test1 = Res.Class.load("sample_simple/test1.txt".AsHaxeString());

            Logger.Information("The content of test1.txt is {text}", test1.toText());

        }

        void IOnGameExit.OnGameExit()
        {
            Logger.Information("Game is exit");
        }
    }
}
