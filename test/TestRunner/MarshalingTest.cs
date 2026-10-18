using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using ModCore.Utilities;

namespace TestRunner
{
    /// <summary>
    /// Round-trip tests for the marshaling layer, covering raw writes/reads,
    /// dynamic writes/reads, the type cache, field-name hashing, the dyn value
    /// pool and the interned-string cache.
    /// </summary>
    public unsafe class MarshalingTest
    {
        public MarshalingTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }

        // Only .NET types with an exact Hashlink counterpart are covered here:
        // bool, u8, u16, i32, i64, f32, f64 and bytes.
        private static readonly Dictionary<string, (Type Type, object Value, object Expected)> PrimitiveCases = new()
        {
            ["bool"] = (typeof(bool), true, true),
            ["byte"] = (typeof(byte), (byte)42, (byte)42),
            ["ushort"] = (typeof(ushort), (ushort)42, (ushort)42),
            ["int"] = (typeof(int), 42, 42),
            ["long"] = (typeof(long), 42L, 42L),
            ["float"] = (typeof(float), 3.5f, 3.5f),
            ["double"] = (typeof(double), 3.5d, 3.5d),
            ["nint"] = (typeof(nint), (nint)42, (nint)42),
        };

        // ---------------------------------------------------------------------
        // Raw write/read for every primitive type
        // ---------------------------------------------------------------------

        [Theory]
        [InlineData("bool")]
        [InlineData("byte")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("long")]
        [InlineData("float")]
        [InlineData("double")]
        [InlineData("nint")]
        public void Raw_WriteRead_AllPrimitiveTypes(string typeName)
        {
            var (type, value, expected) = PrimitiveCases[typeName];
            var hlType = HashlinkMarshal.GetHashlinkType(type);
            Assert.NotNull(hlType);

            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, value, hlType);
            var actual = HashlinkMarshal.ReadData(&buffer, hlType);

            Assert.Equal(expected, actual);
        }

        // ---------------------------------------------------------------------
        // Dynamic write/read for every primitive type
        // ---------------------------------------------------------------------

        [Theory]
        [InlineData("bool")]
        [InlineData("byte")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("long")]
        [InlineData("float")]
        [InlineData("double")]
        [InlineData("nint")]
        public void Dyn_WriteRead_AllPrimitiveTypes(string typeName)
        {
            var (_, value, expected) = PrimitiveCases[typeName];
            var dyn = HashlinkMarshal.GetDyn(value);
            Assert.NotEqual(0, dyn);

            var actual = HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(dyn));

            Assert.Equal(expected, actual);
        }

        // ---------------------------------------------------------------------
        // Type mapping / cache
        // ---------------------------------------------------------------------

        [Fact]
        public void GetHashlinkType_RepeatedCalls_ReturnSameInstance()
        {
            var first = HashlinkMarshal.GetHashlinkType(typeof(double));
            var second = HashlinkMarshal.GetHashlinkType(typeof(double));

            Assert.Same(first, second);
        }

        [Fact]
        public void GetHashlinkType_CachedAndNoCache_AgreeForPrimitives()
        {
            var marshaler = (DefaultHashlinkMarshaler)HashlinkMarshal.DefaultMarshaler;

            foreach (var (type, _, _) in PrimitiveCases.Values)
            {
                var cached = HashlinkMarshal.GetHashlinkType(type);
                var noCache = marshaler.GetHashlinkTypeNoCache(type);

                Assert.NotNull(cached);
                Assert.NotNull(noCache);
                Assert.Equal(cached.TypeKind, noCache.TypeKind);
                Assert.Equal(cached.TypeIndex, noCache.TypeIndex);
            }
        }

        [Fact]
        public void GetHashlinkType_PrimitiveMappings()
        {
            Assert.Equal(TypeKind.HBOOL, HashlinkMarshal.GetHashlinkType(typeof(bool))!.TypeKind);
            Assert.Equal(TypeKind.HUI8, HashlinkMarshal.GetHashlinkType(typeof(byte))!.TypeKind);
            Assert.Equal(TypeKind.HUI16, HashlinkMarshal.GetHashlinkType(typeof(ushort))!.TypeKind);
            Assert.Equal(TypeKind.HI32, HashlinkMarshal.GetHashlinkType(typeof(int))!.TypeKind);
            Assert.Equal(TypeKind.HI64, HashlinkMarshal.GetHashlinkType(typeof(long))!.TypeKind);
            Assert.Equal(TypeKind.HF32, HashlinkMarshal.GetHashlinkType(typeof(float))!.TypeKind);
            Assert.Equal(TypeKind.HF64, HashlinkMarshal.GetHashlinkType(typeof(double))!.TypeKind);
            Assert.Equal(TypeKind.HBYTES, HashlinkMarshal.GetHashlinkType(typeof(nint))!.TypeKind);
        }

        [Fact]
        public void TypeCache_UnknownType_ReturnsNullAndIsNotCached()
        {
            var type = typeof(MarshalingTest);
            var marshaler = (DefaultHashlinkMarshaler)HashlinkMarshal.DefaultMarshaler;

            Assert.Null(HashlinkMarshal.GetHashlinkType(type));
            Assert.Null(HashlinkMarshal.GetHashlinkType(type));
            Assert.Null(marshaler.GetHashlinkTypeNoCache(type));
        }

        [Fact]
        public void TypeCache_DelegateType_IsCached()
        {
            var first = HashlinkMarshal.GetHashlinkType(typeof(Action));
            var second = HashlinkMarshal.GetHashlinkType(typeof(Action));

            Assert.NotNull(first);
            Assert.Same(first, second);
        }

        [Fact]
        public void TypeCache_ObjectType_ReturnsDynamic()
        {
            var type = HashlinkMarshal.GetHashlinkType(typeof(object));

            Assert.NotNull(type);
            Assert.Equal(TypeKind.HDYN, type.TypeKind);
        }

        // ---------------------------------------------------------------------
        // Dynamic value pool
        // ---------------------------------------------------------------------

        [Fact]
        public void DynamicPool_SmallInt_IsReused()
        {
            var first = HashlinkMarshal.GetDyn(42);
            var second = HashlinkMarshal.GetDyn(42);

            Assert.NotEqual(0, first);
            Assert.Equal(first, second);
            Assert.Equal(42, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(first)));
        }

        [Fact]
        public void DynamicPool_Bool_IsReused()
        {
            var first = HashlinkMarshal.GetDyn(true);
            var second = HashlinkMarshal.GetDyn(true);
            var third = HashlinkMarshal.GetDyn(false);

            Assert.Equal(first, second);
            Assert.NotEqual(first, third);
            Assert.Equal(true, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(first)));
            Assert.Equal(false, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(third)));
        }

        [Fact]
        public void DynamicPool_Byte_IsReused()
        {
            var first = HashlinkMarshal.GetDyn((byte)42);
            var second = HashlinkMarshal.GetDyn((byte)42);

            Assert.Equal(first, second);
            Assert.Equal((byte)42, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(first)));
        }

        [Fact]
        public void DynamicPool_UShort_IsReused()
        {
            var first = HashlinkMarshal.GetDyn((ushort)42);
            var second = HashlinkMarshal.GetDyn((ushort)42);

            Assert.Equal(first, second);
            Assert.Equal((ushort)42, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(first)));
        }

        [Fact]
        public void DynamicPool_LargeInt_IsNotReused()
        {
            var pooled = HashlinkMarshal.GetDyn(42);
            var large = HashlinkMarshal.GetDyn(114514);

            Assert.NotEqual(0, large);
            Assert.NotEqual(pooled, large);
            Assert.Equal(114514, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(large)));
        }

        [Fact]
        public void DynamicPool_IntLowerBound_IsReused()
        {
            var first = HashlinkMarshal.GetDyn(-128);
            var second = HashlinkMarshal.GetDyn(-128);

            Assert.NotEqual(0, first);
            Assert.Equal(first, second);
            Assert.Equal(-128, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(first)));
        }

        [Fact]
        public void DynamicPool_IntUpperBound_IsReused()
        {
            var first = HashlinkMarshal.GetDyn(128);
            var second = HashlinkMarshal.GetDyn(128);

            Assert.NotEqual(0, first);
            Assert.Equal(first, second);
            Assert.Equal(128, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(first)));
        }

        [Fact]
        public void DynamicPool_IntBelowRange_FallsBack()
        {
            var lower = HashlinkMarshal.GetDyn(-128);
            var outside = HashlinkMarshal.GetDyn(-129);

            Assert.NotEqual(0, outside);
            Assert.NotEqual(lower, outside);
            Assert.Equal(-129, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(outside)));
        }

        [Fact]
        public void DynamicPool_IntAboveRange_DoesNotThrow()
        {
            var upper = HashlinkMarshal.GetDyn(128);
            var outside = HashlinkMarshal.GetDyn(129);

            Assert.NotEqual(0, outside);
            Assert.NotEqual(upper, outside);
            Assert.Equal(129, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(outside)));
        }

        [Fact]
        public void DynamicPool_UShortZero_IsReused()
        {
            var first = HashlinkMarshal.GetDyn((ushort)0);
            var second = HashlinkMarshal.GetDyn((ushort)0);

            Assert.NotEqual(0, first);
            Assert.Equal(first, second);
            Assert.Equal((ushort)0, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(first)));
        }

        [Fact]
        public void DynamicPool_UShortUpperBound_IsReused()
        {
            var first = HashlinkMarshal.GetDyn((ushort)256);
            var second = HashlinkMarshal.GetDyn((ushort)256);

            Assert.NotEqual(0, first);
            Assert.Equal(first, second);
            Assert.Equal((ushort)256, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(first)));
        }

        [Fact]
        public void DynamicPool_UShortAboveRange_FallsBack()
        {
            var upper = HashlinkMarshal.GetDyn((ushort)256);
            var outside = HashlinkMarshal.GetDyn((ushort)257);

            Assert.NotEqual(0, outside);
            Assert.NotEqual(upper, outside);
            Assert.Equal((ushort)257, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(outside)));
        }

        [Fact]
        public void DynamicPool_UShortMaxValue_FallsBack()
        {
            var upper = HashlinkMarshal.GetDyn((ushort)256);
            var max = HashlinkMarshal.GetDyn(ushort.MaxValue);

            Assert.NotEqual(0, max);
            Assert.NotEqual(upper, max);
            Assert.Equal(ushort.MaxValue, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(max)));
        }

        [Fact]
        public void DynamicPool_ByteMinMax_AreReused()
        {
            var minFirst = HashlinkMarshal.GetDyn(byte.MinValue);
            var minSecond = HashlinkMarshal.GetDyn(byte.MinValue);
            var maxFirst = HashlinkMarshal.GetDyn(byte.MaxValue);
            var maxSecond = HashlinkMarshal.GetDyn(byte.MaxValue);

            Assert.Equal(minFirst, minSecond);
            Assert.Equal(maxFirst, maxSecond);
            Assert.NotEqual(minFirst, maxFirst);
            Assert.Equal(byte.MinValue, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(minFirst)));
            Assert.Equal(byte.MaxValue, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(maxFirst)));
        }

        [Fact]
        public void DynamicPool_SmallInt_SurvivesHashlinkGc()
        {
            var pooled = HashlinkMarshal.GetDyn(42);
            Assert.NotEqual(0, pooled);

            HashlinkNative.hl_gc_major();

            var actual = HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(pooled));
            Assert.Equal(42, actual);
        }

        // ---------------------------------------------------------------------
        // Field access: string name cache vs hashed name
        // ---------------------------------------------------------------------

        [Fact]
        public void FieldAccess_StringName_MatchesHashedName()
        {
            var point = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point")
                .CreateInstance();

            int hash;
            fixed (char* name = "x")
            {
                hash = HashlinkNative.hl_hash_gen(name, false);
            }

            for (var i = 0; i < 100; i++)
            {
                point.SetFieldValue("x", (double)i);

                Assert.Equal((double)i, point.GetFieldValue("x"));
                Assert.Equal((double)i, point.GetFieldValue(hash));
            }
        }

        [Fact]
        public void FieldAccess_MultipleFields_RepeatedAccess()
        {
            var point = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point")
                .CreateInstance();

            for (var i = 0; i < 100; i++)
            {
                point.SetFieldValue("x", (double)i);
                point.SetFieldValue("y", (double)(i + 1));

                Assert.Equal((double)i, point.GetFieldValue("x"));
                Assert.Equal((double)(i + 1), point.GetFieldValue("y"));
            }
        }

        // ---------------------------------------------------------------------
        // Interned string cache
        // ---------------------------------------------------------------------

        [Fact]
        public void AsHaxeString_InternedLiteral_ReturnsCachedInstance()
        {
            var first = "marshaling.interned.literal".AsHaxeString();
            var second = "marshaling.interned.literal".AsHaxeString();

            Assert.Same(first, second);
            Assert.Equal("marshaling.interned.literal", first.ToString());
        }

        [Fact]
        public void AsHaxeString_NonInternedString_ReturnsNewInstance()
        {
            var text = "marshaling.noninterned." + Guid.NewGuid().ToString("N");

            var first = text.AsHaxeString();
            var second = text.AsHaxeString();

            Assert.NotSame(first, second);
            Assert.Equal(text, first.ToString());
            Assert.Equal(text, second.ToString());
        }

        [Fact]
        public void AsHaxeString_EmptyString_ReturnsCachedInstance()
        {
            var first = string.Empty.AsHaxeString();
            var second = string.Empty.AsHaxeString();

            Assert.Same(first, second);
            Assert.Equal("", first.ToString());
        }

        [Fact]
        public void AsHaxeString_ValueInternedString_ReturnsCachedInstance()
        {
            var text = new string("marshaling.interned.literal".ToCharArray());
            Assert.NotNull(string.IsInterned(text));

            var first = text.AsHaxeString();
            var second = text.AsHaxeString();

            Assert.Same(first, second);
            Assert.Equal(text, first.ToString());
        }

        // ---------------------------------------------------------------------
        // Primitive edge cases
        // ---------------------------------------------------------------------

        private static object? RawRoundTrip(Type type, object value)
        {
            var hlType = HashlinkMarshal.GetHashlinkType(type);
            Assert.NotNull(hlType);

            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, value, hlType);
            return HashlinkMarshal.ReadData(&buffer, hlType);
        }

        private static object? DynRoundTrip(object value)
        {
            var dyn = HashlinkMarshal.GetDyn(value);
            Assert.NotEqual(0, dyn);
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(dyn));
        }

        [Fact]
        public void Raw_WriteRead_Int32_MinMax()
        {
            Assert.Equal(int.MinValue, RawRoundTrip(typeof(int), int.MinValue));
            Assert.Equal(int.MaxValue, RawRoundTrip(typeof(int), int.MaxValue));
        }

        [Fact]
        public void Dyn_WriteRead_Int32_MinMax()
        {
            Assert.Equal(int.MinValue, DynRoundTrip(int.MinValue));
            Assert.Equal(int.MaxValue, DynRoundTrip(int.MaxValue));
        }

        [Fact]
        public void Raw_WriteRead_Int64_MinMax()
        {
            Assert.Equal(long.MinValue, RawRoundTrip(typeof(long), long.MinValue));
            Assert.Equal(long.MaxValue, RawRoundTrip(typeof(long), long.MaxValue));
        }

        [Fact]
        public void Dyn_WriteRead_Int64_MinMax()
        {
            Assert.Equal(long.MinValue, DynRoundTrip(long.MinValue));
            Assert.Equal(long.MaxValue, DynRoundTrip(long.MaxValue));
        }

        [Fact]
        public void Raw_WriteRead_Byte_MinMax()
        {
            Assert.Equal(byte.MinValue, RawRoundTrip(typeof(byte), byte.MinValue));
            Assert.Equal(byte.MaxValue, RawRoundTrip(typeof(byte), byte.MaxValue));
        }

        [Fact]
        public void Dyn_WriteRead_Byte_MinMax()
        {
            Assert.Equal(byte.MinValue, DynRoundTrip(byte.MinValue));
            Assert.Equal(byte.MaxValue, DynRoundTrip(byte.MaxValue));
        }

        [Fact]
        public void Raw_WriteRead_UShort_MinMax()
        {
            Assert.Equal(ushort.MinValue, RawRoundTrip(typeof(ushort), ushort.MinValue));
            Assert.Equal(ushort.MaxValue, RawRoundTrip(typeof(ushort), ushort.MaxValue));
        }

        [Fact]
        public void Dyn_WriteRead_UShort_MinMax()
        {
            Assert.Equal(ushort.MinValue, DynRoundTrip(ushort.MinValue));
            Assert.Equal(ushort.MaxValue, DynRoundTrip(ushort.MaxValue));
        }

        [Fact]
        public void Raw_WriteRead_Float_SpecialValues()
        {
            Assert.True(float.IsNaN((float)RawRoundTrip(typeof(float), float.NaN)!));
            Assert.Equal(float.PositiveInfinity, RawRoundTrip(typeof(float), float.PositiveInfinity));
            Assert.Equal(float.NegativeInfinity, RawRoundTrip(typeof(float), float.NegativeInfinity));
        }

        [Fact]
        public void Dyn_WriteRead_Float_SpecialValues()
        {
            Assert.True(float.IsNaN((float)DynRoundTrip(float.NaN)!));
            Assert.Equal(float.PositiveInfinity, DynRoundTrip(float.PositiveInfinity));
            Assert.Equal(float.NegativeInfinity, DynRoundTrip(float.NegativeInfinity));
        }

        [Fact]
        public void Raw_WriteRead_Double_SpecialValues()
        {
            Assert.True(double.IsNaN((double)RawRoundTrip(typeof(double), double.NaN)!));
            Assert.Equal(double.PositiveInfinity, RawRoundTrip(typeof(double), double.PositiveInfinity));
            Assert.Equal(double.NegativeInfinity, RawRoundTrip(typeof(double), double.NegativeInfinity));
        }

        [Fact]
        public void Dyn_WriteRead_Double_SpecialValues()
        {
            Assert.True(double.IsNaN((double)DynRoundTrip(double.NaN)!));
            Assert.Equal(double.PositiveInfinity, DynRoundTrip(double.PositiveInfinity));
            Assert.Equal(double.NegativeInfinity, DynRoundTrip(double.NegativeInfinity));
        }

        // ---------------------------------------------------------------------
        // Nullable primitive (HNULL) raw marshaling
        // ---------------------------------------------------------------------

        [Fact]
        public void Raw_WriteRead_NullableInt_Value()
        {
            var type = HashlinkMarshal.GetHashlinkType(typeof(int?));
            Assert.NotNull(type);
            Assert.Equal(TypeKind.HNULL, type.TypeKind);

            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, (int?)42, type);
            var actual = HashlinkMarshal.ReadData(&buffer, type);

            Assert.Equal(42, actual);
        }

        [Fact]
        public void Raw_WriteRead_NullableInt_Null()
        {
            var type = HashlinkMarshal.GetHashlinkType(typeof(int?));
            Assert.NotNull(type);
            Assert.Equal(TypeKind.HNULL, type.TypeKind);

            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, (int?)null, type);
            var actual = HashlinkMarshal.ReadData(&buffer, type);

            Assert.Null(actual);
        }
    }
}
