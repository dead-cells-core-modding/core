using dc.hl;
using dc.hxd.fs;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using HaxeProxy.Runtime;

namespace TestRunner
{
    public class ObjectInheritanceTest
    {
        private class TestObject : FileEntry
        {
            public class StaticClass : Class
            {
                public int TEST_S_VAL_H = 123;
            }
            public new static StaticClass Class { get; } = new();

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
            public static int TEST_S_VAL = 3;
            public int TEST_VAL = 2;
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
            HashlinkMarshal.EnsureThreadRegistered();

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
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = new TestObject2();

            Assert.Equal(114515, obj.getSign());
            Assert.True(obj.overrideMethodHasBennCalled);

            var hobj = (HashlinkObject)obj.HashlinkObj;
            var cl = hobj.GetFieldValue("getSign") as HashlinkClosure;
            Assert.NotNull(cl);
            Assert.Equal(114515, cl.DynamicInvoke());

            var dyn = hobj.AsDynamic();
            Assert.Equal(114515, dyn.getSign());
            Assert.Equal(2, (int) dyn.TEST_VAL);

            dynamic gcl = HaxeProxyUtils.GetClass<Class>(typeof(TestObject2)).HashlinkObj;
            Assert.Equal(3, (int)gcl.TEST_S_VAL);

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
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = new TestObject();

            dynamic gcl = HaxeProxyUtils.GetClass<Class>(typeof(TestObject)).HashlinkObj;
            Assert.Equal(123, (int)gcl.TEST_S_VAL_H);
            Assert.Equal(TestObject.Class, (HaxeProxyBase)gcl);

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
