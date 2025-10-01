using dc.haxe.xml._Access;
using dc.hxd.fs;
using Hashlink.Marshaling;
using Hashlink.Proxy.DynamicAccess;
using HaxeProxy.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestRunner
{
    public class ExceptionTest
    {
        public ExceptionTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }
        private class TestObject : FileEntry
        {
            public bool overrideMethodHasBennCalled = false;
            public override int getSign()
            {
                overrideMethodHasBennCalled = true;
                return 114514;
            }
            public override void load(HlAction onReady)
            {
                onReady();
            }
        }

       // [Fact]
        public void Test_ExceptionThrow()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var to = new TestObject();
            var da = to.HashlinkObj.AsDynamic();

            try
            {
                da.load((HlAction)(() =>
                {
                    throw new Exception("Test Exception");
                }));
                Assert.Fail();
            }
            catch (TargetInvocationException ex)
            {
                Assert.Equal("Test Exception", ex.InnerException?.Message);
                Console.WriteLine("BBB");
            }

        }
    }
}
