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
            public override void load(HlAction onReady)
            {
                onReady();
            }
        }
        private class TestObject2 : TestObject
        {
            public override int getSign()
            {
                return base.getSign() + 1;
            }
            public override void load(HlAction onReady)
            {
                overrideMethodHasBennCalled = true;
                base.load(onReady);
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
        public void Test_Override_2()
        {
            var obj = new TestObject2();

            Assert.Equal(114515, obj.getSign());
            Assert.True(obj.overrideMethodHasBennCalled);

            var hobj = (HashlinkObject)obj.HashlinkObj;
            var cl = hobj.GetFieldValue("getSign") as HashlinkClosure;
            Assert.NotNull(cl);
            Assert.Equal(114515, cl.DynamicInvoke());

            var dyn = hobj.AsDynamic();
            Assert.Equal(114515, dyn.getSign());

            var isFailed = true;
            obj.overrideMethodHasBennCalled = false;
            dyn.load((Action)(() =>
            {
                isFailed = false;
            }));
            Assert.False(isFailed);
            Assert.True(obj.overrideMethodHasBennCalled);
        }
        [Fact]
        public void Test_Override()
        {
            var obj = new TestObject();

            Assert.Equal(114514, obj.getSign());
            Assert.True(obj.overrideMethodHasBennCalled);

            var hobj = (HashlinkObject)obj.HashlinkObj;
            var cl = hobj.GetFieldValue("getSign") as HashlinkClosure;
            Assert.NotNull(cl);
            Assert.Equal(114514, cl.DynamicInvoke());

            var dyn = hobj.AsDynamic();
            Assert.Equal(114514, dyn.getSign());

            var isFailed = true;
            dyn.load((Action)(() =>
            {
                isFailed = false;
            }));
            Assert.False(isFailed);

        }
    }
}
