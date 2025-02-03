using LibraryMod;
using ModCore.Mods;

namespace SampleSimple
{
    public class SimpleMod(ModInfo info) : ModBase(info)
    {
        public override void Initialize()
        {
            Logger.Information("Hello, World!");

            HelloWorld.Say();
        }
    }
}
