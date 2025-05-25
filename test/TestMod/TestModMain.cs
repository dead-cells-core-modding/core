using ModCore.Mods;

namespace TestMod
{
    public class TestModMain(ModInfo info) : ModBase(info)
    {
        public static bool modIsLoaded = false;

        public override void Initialize()
        {
            modIsLoaded = true;

            dc.haxe.Log.Class.trace = (obj, info) =>
            {

            };
        }
    }
}
