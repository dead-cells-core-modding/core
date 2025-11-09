using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Console.WriteLine("Init game context");
            gameContext = new();
        }
    }
}
