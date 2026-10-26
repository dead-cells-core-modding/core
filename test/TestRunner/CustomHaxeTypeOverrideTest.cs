using dc.hl;
using dc.hxd.fs;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.DynamicAccess;
using HaxeProxy.Runtime;

namespace TestRunner
{
    /// <summary>
    /// Tests multiple and multi-level overrides on custom Haxe types.
    /// </summary>
    public unsafe class CustomHaxeTypeOverrideTest
    {
        private class OverrideBase : FileEntry
        {
            public override int getSign()
            {
                return 1;
            }

            public override void load(HlAction onReady)
            {
                onReady();
            }
        }

        private class OverrideMiddle : OverrideBase
        {
            public override int getSign()
            {
                return 2;
            }
        }

        private class OverrideLeaf : OverrideMiddle
        {
            public override int getSign()
            {
                return 3;
            }

            public override void load(HlAction onReady)
            {
                onReady();
            }
        }

        public CustomHaxeTypeOverrideTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }

        [Fact]
        public void CustomType_MultiLevelOverride_UsesMostDerivedMethod()
        {
            var baseObject = new OverrideBase();
            var middleObject = new OverrideMiddle();
            var leafObject = new OverrideLeaf();

            Assert.Equal(1, baseObject.getSign());
            Assert.Equal(2, middleObject.getSign());
            Assert.Equal(3, leafObject.getSign());

            Assert.Equal(1, (int)baseObject.HashlinkObj.AsDynamic().getSign());
            Assert.Equal(2, (int)middleObject.HashlinkObj.AsDynamic().getSign());
            Assert.Equal(3, (int)leafObject.HashlinkObj.AsDynamic().getSign());

            for (var i = 0; i < 100; i++)
            {
                Assert.Equal(3, (int)leafObject.HashlinkObj.AsDynamic().getSign());
            }

            GC.KeepAlive(baseObject);
            GC.KeepAlive(middleObject);
            GC.KeepAlive(leafObject);
        }

        [Fact]
        public void CustomType_MultipleOverriddenMethods_AreBothUsed()
        {
            var value = new OverrideLeaf();
            var dynamicObject = value.HashlinkObj.AsDynamic();

            Assert.Equal(3, (int)dynamicObject.getSign());

            var loadCalled = false;
            dynamicObject.load((Action)(() => loadCalled = true));

            Assert.True(loadCalled);

            var hashlinkObject = (HashlinkObject)value.HashlinkObj;
            var closure = hashlinkObject.GetFieldValue("getSign") as Hashlink.Proxy.Clousre.HashlinkClosure;
            Assert.NotNull(closure);
            Assert.Equal(3, (int)closure.DynamicInvoke()!);

            GC.KeepAlive(value);
        }

        [Fact]
        public void CustomType_VirtualDispatch_ThroughBaseReference()
        {
            OverrideBase value = new OverrideLeaf();

            // C# virtual dispatch.
            Assert.Equal(3, value.getSign());

            // The Haxe proxy is cached as the most-derived instance.
            var proxy = value.HashlinkObj.AsHaxe<OverrideBase>();
            Assert.Same(value, proxy);
            Assert.Equal(3, proxy.getSign());

            var middleProxy = value.HashlinkObj.AsHaxe<OverrideMiddle>();
            Assert.Same(value, middleProxy);
            Assert.Equal(3, middleProxy.getSign());

            var dynamicObject = value.HashlinkObj.AsDynamic();
            Assert.Equal(3, (int)dynamicObject.getSign());

            var hashlinkObject = (HashlinkObject)value.HashlinkObj;
            var closure = hashlinkObject.GetFieldValue("getSign") as Hashlink.Proxy.Clousre.HashlinkClosure;
            Assert.NotNull(closure);
            Assert.Equal(3, (int)closure.DynamicInvoke()!);

            GC.KeepAlive(value);
        }
        [Fact]
        public void CustomType_DerivedAsHaxe_PreservesRuntimeType()
        {
            var value = new OverrideLeaf();
            var proxy = value.HashlinkObj.AsHaxe<OverrideLeaf>();

            Assert.Same(value, proxy);
            Assert.Equal(value.HashlinkPointer, proxy.HashlinkPointer);
            Assert.Equal(3, proxy.getSign());

            GC.KeepAlive(value);
        }
    }
}

