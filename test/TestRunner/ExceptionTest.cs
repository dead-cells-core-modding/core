using dc.hxd.fs;
using Hashlink.Marshaling;
using Hashlink.Proxy.DynamicAccess;
using HashlinkNET.Native.Impl;
using HaxeProxy.Runtime;
using System.Reflection;
using System.Runtime.InteropServices;

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

        [Fact]
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
            }

        }
    }
}
