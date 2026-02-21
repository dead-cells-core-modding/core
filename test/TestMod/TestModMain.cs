using HaxeProxy.Runtime;
using ModCore.Mods;

namespace TestMod
{
    public class TestModMain(ModInfo info) : ModBase(info)
    {
        public static bool modIsLoaded = false;

        public override void Initialize()
        {
            Ref<int> a = Ref<int>.In(32);
            modIsLoaded = true;
        }
    }
}
