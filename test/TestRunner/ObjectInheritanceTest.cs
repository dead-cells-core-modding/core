using dc.h2d;
using dc.hxd.fs;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using HaxeProxy.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRunner
{
    public class ObjectInheritanceTest
    {
        private class TestObject : FileEntry
        {
            public bool overrideMethodHasBennCalled = false;
            public override int getSign()
            {
                overrideMethodHasBennCalled = true;
                return 114514;
            } 
        }
        [Fact]
        public void Test_Marshal()
        {
            var obj = new TestObject();
            var ptr = obj.HashlinkPointer;

            var hobj = (HashlinkObject?) HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(ptr));
            Assert.NotNull(hobj);
            Assert.Equal(obj.HashlinkObj, hobj);
            Assert.Equal(obj, hobj.AsHaxe<TestObject>());
        }
        [Fact]
        public void Test_Override()
        {
            var obj = new TestObject();

            Assert.Equal(114514, obj.getSign());
            Assert.True(obj.overrideMethodHasBennCalled);

            var hobj = (HashlinkObject) obj.HashlinkObj;
            var cl = hobj.GetFieldValue("getSign") as HashlinkClosure;
            Assert.NotNull(cl);
            Assert.Equal(114514, cl.DynamicInvoke());

            Assert.Equal(114514, hobj.AsDynamic().getSign());
        }
    }
}
