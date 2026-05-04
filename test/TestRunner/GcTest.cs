using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;

using static Hashlink.HashlinkNative;

namespace TestRunner
{
    public unsafe class GcTest
    {
        private class HashlinkObjPtr(nint ptr) : IHashlinkPointer
        {
            public nint HashlinkPointer => ptr;
        }
        [Fact]
        public void Alloc()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var str = new HashlinkString("Test String");
            Assert.True(str.IsValid);
            Assert.Equal("Test String", str.ToString());
        }

        [Fact]
        public void KeepAlive()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var str = new HashlinkString("Test String");
            Assert.True(str.IsValid);
            Assert.Equal("Test String", str.ToString());

            hl_gc_major();

            var str2 = new HashlinkString("Test String 2");

            Assert.True(str.IsValid);
            Assert.Equal("Test String", str.ToString());

            GC.KeepAlive(str2);
        }

        [Fact]
        public void KeepAlive2()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = new HashlinkDynObj();
            var item0 = (nint)hl_alloc_dynobj();
            obj.SetFieldValue("test", new HashlinkObjPtr(item0));
            obj.SetFieldValue("test2", obj);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            hl_gc_major();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var item01 = obj.GetFieldValue("test") as HashlinkObj;
            Assert.NotNull(item01);
            Assert.Equal(item01.HashlinkPointer, item0);
            Assert.True(hl_gc_get_memsize((void*)item0) > 0);
            Assert.True(item01.IsValid);
        }
    }
}
