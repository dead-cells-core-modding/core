using System.Reflection;
using TestRunner;
using Xunit.v3;

[assembly: TestFramework(typeof(DCCMTestFramework))]

namespace TestRunner
{
    internal class DCCMTestFramework : XunitTestFramework
    {
       
        public DCCMTestFramework()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DCCM_TEST_RERUN")))
            {
                Environment.Exit(0); //fuck. I dont know why
            }
            Environment.SetEnvironmentVariable("DCCM_TEST_RERUN", "1");
        }
    }
}
