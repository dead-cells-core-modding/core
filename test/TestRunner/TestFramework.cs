using TestRunner;
using Xunit.v3;

[assembly: TestFramework(typeof(DCCMTestFramework))]

namespace TestRunner
{
    internal class DCCMTestFramework : XunitTestFramework
    {
        private readonly GameContext gameContext;
        public DCCMTestFramework()
        {
            //Console.WriteLine("Init game context");
            gameContext = new();
        }
    }
}
