using Hashlink.Marshaling;
using TestMod;

namespace TestRunner
{
    public class ModLoaderTest
    {
        [Fact]
        public void ModLoader_LoadMod()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            Assert.True(TestModMain.modIsLoaded);
        }
    }
}
