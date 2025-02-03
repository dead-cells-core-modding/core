using LibraryMod;
using ModCore.Events.Interfaces.Game;
using ModCore.Mods;

namespace SampleSimple
{
    public class SimpleMod(ModInfo info) : ModBase(info),
        IOnGameExit
    {
        public override void Initialize()
        {
            Logger.Information("Hello, World!");

            HelloWorld.Say();
        }

        void IOnGameExit.OnGameExit()
        {
            Logger.Information("Game is exit");
        }
    }
}
