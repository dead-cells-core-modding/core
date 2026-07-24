using dc;
using dc.pr;
using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime;
using ModCore.Utilities;

namespace TestRunner
{
    /// <summary>
    /// Tests for <c>int?</c> (nullable int) handling in the HaxeProxy layer —
    /// read from proxy fields, return via hooked methods, pass as arguments
    /// through hooked methods, and read/write through HashlinkMarshal.
    ///
    /// All method calls use hooks to intercept execution, since the test
    /// environment runs an incomplete game where direct method execution
    /// would fail.
    /// </summary>
    public unsafe class HaxeProxyNullableIntTest
    {
        // =========================================================================
        // Helper
        // =========================================================================

        private static Game CreateGame()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            return HaxeProxyUtils.GetHashlinkType(typeof(Game))
                .CreateInstance()
                .AsHaxe<Game>();
        }

        // =========================================================================
        // Read: reading int? from a proxy field (no hook needed)
        // =========================================================================

        [Fact]
        public void Read_NullableInt_Field()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var gm = CreateGame();
            int? depth = gm.shopMimicBiomeDepth;

            Assert.True(depth is null or >= 0);
        }

        [Fact]
        public void Read_NullableInt_Field_Consistent()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var gm = CreateGame();
            int? d1 = gm.shopMimicBiomeDepth;
            int? d2 = gm.shopMimicBiomeDepth;

            Assert.Equal(d1, d2);
        }

        // =========================================================================
        // Return: method returning int? (intercepted via hook)
        // =========================================================================

        [Fact]
        public void Return_NullableInt_NonNull()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            Hook_Game.getBiomeVisitCount += (orig, self, id) => 42;

            try
            {
                var gm = CreateGame();
                int? result = gm.getBiomeVisitCount("pa_tuto".AsHaxeString());
                Assert.Equal(42, result);
            }
            finally
            {
                Hook_Game.getBiomeVisitCount -= (orig, self, id) => 42;
            }
        }

        [Fact]
        public void Return_NullableInt_Null()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            Hook_Game.getBiomeVisitCount += (orig, self, id) => (int?)null;

            try
            {
                var gm = CreateGame();
                int? result = gm.getBiomeVisitCount("any_id".AsHaxeString());
                Assert.Null(result);
            }
            finally
            {
                Hook_Game.getBiomeVisitCount -= (orig, self, id) => (int?)null;
            }
        }

        [Fact]
        public void Return_NullableInt_Negative()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            Hook_Game.getBiomeVisitCount += (orig, self, id) => -1;

            try
            {
                var gm = CreateGame();
                int? result = gm.getBiomeVisitCount("any_id".AsHaxeString());
                Assert.Equal(-1, result);
            }
            finally
            {
                Hook_Game.getBiomeVisitCount -= (orig, self, id) => -1;
            }
        }

        [Fact]
        public void Return_NullableInt_Zero()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            Hook_Game.getBiomeVisitCount += (orig, self, id) => 0;

            try
            {
                var gm = CreateGame();
                int? result = gm.getBiomeVisitCount("any_id".AsHaxeString());
                Assert.Equal(0, result);
            }
            finally
            {
                Hook_Game.getBiomeVisitCount -= (orig, self, id) => 0;
            }
        }

        [Fact]
        public void Return_NullableInt_LargeValue()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            Hook_Game.getBiomeVisitCount += (orig, self, id) => int.MaxValue;

            try
            {
                var gm = CreateGame();
                int? result = gm.getBiomeVisitCount("any_id".AsHaxeString());
                Assert.Equal(int.MaxValue, result);
            }
            finally
            {
                Hook_Game.getBiomeVisitCount -= (orig, self, id) => int.MaxValue;
            }
        }

        // =========================================================================
        // Arg: passing int? as argument (intercepted via hook)
        // =========================================================================

        [Fact]
        public void Arg_NullableInt_NonNull()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            int? capturedArg = null;
            Hook_Game.onActPressed += (orig, self, act, isKey) => capturedArg = act;

            var gm = CreateGame();
            gm.onActPressed(42, false);

            Assert.NotNull(capturedArg);
            Assert.Equal(42, capturedArg);
        }

        [Fact]
        public void Arg_NullableInt_Null()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            int? capturedArg = -1;
            Hook_Game.onActPressed += (orig, self, act, isKey) => capturedArg = act;

            var gm = CreateGame();
            gm.onActPressed(null, false);

            Assert.Null(capturedArg);
        }

        [Fact]
        public void Arg_NullableInt_Zero()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            int? capturedArg = null;
            Hook_Game.onActPressed += (orig, self, act, isKey) => capturedArg = act;

            var gm = CreateGame();
            gm.onActPressed(0, false);

            Assert.NotNull(capturedArg);
            Assert.Equal(0, capturedArg);
        }

        [Fact]
        public void Arg_NullableInt_Negative()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            int? capturedArg = null;
            Hook_Game.onActPressed += (orig, self, act, isKey) => capturedArg = act;

            var gm = CreateGame();
            gm.onActPressed(-100, false);

            Assert.NotNull(capturedArg);
            Assert.Equal(-100, capturedArg);
        }

        [Fact]
        public void Arg_NullableInt_WithBoolTrue()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            int? capturedAct = null;
            bool capturedKey = false;
            Hook_Game.onActPressed += (orig, self, act, isKey) =>
            {
                capturedAct = act;
                capturedKey = isKey;
            };

            var gm = CreateGame();
            gm.onActPressed(7, true);

            Assert.Equal(7, capturedAct);
            Assert.True(capturedKey);
        }

        // =========================================================================
        // Write + Read: HashlinkDynObj roundtrip for int?
        // =========================================================================

        [Fact]
        public void WriteRead_NullableInt_ThroughDynObj_NonNull()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = new HashlinkDynObj();
            dyn.SetFieldValue("val", 42);
            Assert.Equal(42, (int?)dyn.GetFieldValue("val"));
        }

        [Fact]
        public void WriteRead_NullableInt_ThroughDynObj_Null()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = new HashlinkDynObj();
            dyn.SetFieldValue("val", (int?)null);
            Assert.Null(dyn.GetFieldValue("val"));
        }

        [Fact]
        public void WriteRead_NullableInt_ThroughDynObj_Negative()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = new HashlinkDynObj();
            dyn.SetFieldValue("val", -100);
            Assert.Equal(-100, (int?)dyn.GetFieldValue("val"));
        }

        [Fact]
        public void WriteRead_NullableInt_ThroughDynObj_Zero()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = new HashlinkDynObj();
            dyn.SetFieldValue("val", 0);
            Assert.Equal(0, (int?)dyn.GetFieldValue("val"));
        }

        [Fact]
        public void WriteRead_NullableInt_ThroughDynObj_Overwrite()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = new HashlinkDynObj();

            dyn.SetFieldValue("val", 100);
            Assert.Equal(100, (int?)dyn.GetFieldValue("val"));

            dyn.SetFieldValue("val", 200);
            Assert.Equal(200, (int?)dyn.GetFieldValue("val"));

            dyn.SetFieldValue("val", (int?)null);
            Assert.Null(dyn.GetFieldValue("val"));

            dyn.SetFieldValue("val", 300);
            Assert.Equal(300, (int?)dyn.GetFieldValue("val"));
        }

        // =========================================================================
        // Write + Read: HashlinkMarshal.GetDyn roundtrip
        // =========================================================================

        [Fact]
        public void WriteRead_NullableInt_GetDyn_NonNull()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            nint dynPtr = HashlinkMarshal.GetDyn(77);
            Assert.NotEqual(0, dynPtr);

            object? result = HashlinkMarshal.ReadData(
                &dynPtr, HashlinkMarshal.Module.KnownTypes.Dynamic);
            Assert.Equal(77, result);
        }

        [Fact]
        public void WriteRead_NullableInt_GetDyn_Null()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            nint dynPtr = HashlinkMarshal.GetDyn((int?)null);
            Assert.Equal(0, dynPtr);
        }

        // =========================================================================
        // Type system: Null<Int> HNULL type verification
        // =========================================================================

        [Fact]
        public void Read_NullableInt_NativeNullType()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var nullIntType = HashlinkMarshal.Module.GetTypeByName("null<i32>");
            Assert.NotNull(nullIntType);
            Assert.Equal(TypeKind.HNULL, nullIntType.TypeKind);
            Assert.True(nullIntType.IsNull);
        }
    }
}
