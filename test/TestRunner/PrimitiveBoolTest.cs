using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Wrapper;

using static Hashlink.HashlinkNative;

namespace TestRunner
{
    /// <summary>
    /// Hashlink booleans are bytes, but only 0 is false. Any non-zero byte
    /// must be observed as a normal managed bool at every boundary.
    /// </summary>
    public unsafe class PrimitiveBoolTest
    {
        private sealed class RawPtr(nint ptr) : IHashlinkPointer
        {
            public nint HashlinkPointer => ptr;
        }

        public PrimitiveBoolTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }

        [Fact]
        public void ReadData_NormalizesNonStandardBool()
        {
            var boolType = HashlinkMarshal.Module.KnownTypes.Bool;

            foreach (var raw in new byte[] { 0, 1, 2, 0x7F, 0xFF })
            {
                var buffer = raw;
                var result = HashlinkMarshal.ReadData(&buffer, boolType);

                Assert.IsType<bool>(result);
                Assert.Equal(raw != 0, (bool)result!);
            }
        }

        [Fact]
        public void WriteData_NormalizesBoolToZeroOrOne()
        {
            var boolType = HashlinkMarshal.Module.KnownTypes.Bool;
            var buffer = (byte)0xFF;

            HashlinkMarshal.WriteData(&buffer, true, boolType);
            Assert.Equal(1, buffer);

            buffer = 0xFF;
            HashlinkMarshal.WriteData(&buffer, false, boolType);
            Assert.Equal(0, buffer);
        }

        [Fact]
        public void DynamicField_ReadNormalizesNonStandardBool()
        {
            var boolType = HashlinkMarshal.Module.KnownTypes.Bool;
            var dyn = hl_alloc_dynamic(boolType.NativeType);
            ((HL_vdynamic*)dyn)->val.ui8 = 2;

            var obj = new HashlinkDynObj();
            obj.SetFieldValue("flag", new RawPtr((nint)dyn));

            var value = obj.GetFieldValue("flag");

            Assert.IsType<bool>(value);
            Assert.True((bool)value!);

            GC.KeepAlive(obj);
        }

        [Fact]
        public void CallbackCheckBool_NormalizesNonStandardBool()
        {
            Assert.False(WrapperHelper.CheckBool(0));
            Assert.True(WrapperHelper.CheckBool(1));
            Assert.True(WrapperHelper.CheckBool(2));
            Assert.True(WrapperHelper.CheckBool(-1));
        }
    }
}
