using BenchmarkDotNet.Attributes;
using dc.h2d.col;
using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using ModCore.Utilities;

namespace DCCMBenchmark
{
    /// <summary>
    /// Marshaling cache and boundary benchmarks: every primitive type that has
    /// an exact Hashlink counterpart, the dynamic value pool hit/miss paths,
    /// type resolution cache, field-name hash cache and interned string cache.
    /// </summary>
    [MemoryDiagnoser]
    public unsafe class MarshalingCacheBenchmarks
    {
        private HashlinkObject rawPoint = null!;
        private int fieldHash;
        private nint fieldOffset;
        private string internedText = null!;
        private string nonInternedText = null!;
        private DefaultHashlinkMarshaler marshaler = null!;

        private HashlinkType boolType = null!;
        private HashlinkType u8Type = null!;
        private HashlinkType u16Type = null!;
        private HashlinkType i32Type = null!;
        private HashlinkType i64Type = null!;
        private HashlinkType f32Type = null!;
        private HashlinkType f64Type = null!;
        private HashlinkType bytesType = null!;

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            var known = HashlinkMarshal.Module.KnownTypes;
            boolType = known.Bool;
            u8Type = known.I8;
            u16Type = known.I16;
            i32Type = known.I32;
            i64Type = known.I64;
            f32Type = known.F32;
            f64Type = known.F64;
            bytesType = known.Bytes;
            marshaler = (DefaultHashlinkMarshaler)HashlinkMarshal.DefaultMarshaler;

            rawPoint = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point")
                .CreateInstance();
            rawPoint.SetFieldValue("x", 3d);

            fixed (char* name = "x")
            {
                fieldHash = HashlinkNative.hl_hash_gen(name, false);
            }

            HL_type* fieldType;
            var fieldPtr = (nint)HashlinkNative.hl_obj_lookup(
                (HL_vdynamic*)rawPoint.HashlinkPointer,
                fieldHash,
                out fieldType);
            fieldOffset = fieldPtr - rawPoint.HashlinkPointer;

            internedText = "marshaling.benchmark.interned";
            nonInternedText = "marshaling.benchmark.noninterned." + Guid.NewGuid().ToString("N");
        }

        // ---------------------------------------------------------------------
        // Raw primitive round-trips
        // ---------------------------------------------------------------------

        [Benchmark]
        public object? Raw_RoundTrip_Bool()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, true, boolType);
            return HashlinkMarshal.ReadData(&buffer, boolType);
        }

        [Benchmark]
        public object? Raw_RoundTrip_Byte()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, (byte)42, u8Type);
            return HashlinkMarshal.ReadData(&buffer, u8Type);
        }

        [Benchmark]
        public object? Raw_RoundTrip_UShort()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, (ushort)42, u16Type);
            return HashlinkMarshal.ReadData(&buffer, u16Type);
        }

        [Benchmark]
        public object? Raw_RoundTrip_Int()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, 42, i32Type);
            return HashlinkMarshal.ReadData(&buffer, i32Type);
        }

        [Benchmark]
        public object? Raw_RoundTrip_Long()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, 42L, i64Type);
            return HashlinkMarshal.ReadData(&buffer, i64Type);
        }

        [Benchmark]
        public object? Raw_RoundTrip_Float()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, 3.5f, f32Type);
            return HashlinkMarshal.ReadData(&buffer, f32Type);
        }

        [Benchmark]
        public object? Raw_RoundTrip_Double()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, 3.5d, f64Type);
            return HashlinkMarshal.ReadData(&buffer, f64Type);
        }

        [Benchmark]
        public object? Raw_RoundTrip_NInt()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, (nint)42, bytesType);
            return HashlinkMarshal.ReadData(&buffer, bytesType);
        }

        // ---------------------------------------------------------------------
        // Dynamic primitive round-trips
        // ---------------------------------------------------------------------

        [Benchmark]
        public object? Dyn_RoundTrip_Bool()
        {
            var dyn = HashlinkMarshal.GetDyn(true);
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(dyn));
        }

        [Benchmark]
        public object? Dyn_RoundTrip_Byte()
        {
            var dyn = HashlinkMarshal.GetDyn((byte)42);
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(dyn));
        }

        [Benchmark]
        public object? Dyn_RoundTrip_UShort()
        {
            var dyn = HashlinkMarshal.GetDyn((ushort)42);
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(dyn));
        }

        [Benchmark]
        public object? Dyn_RoundTrip_Int()
        {
            var dyn = HashlinkMarshal.GetDyn(42);
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(dyn));
        }

        [Benchmark]
        public object? Dyn_RoundTrip_Long()
        {
            var dyn = HashlinkMarshal.GetDyn(42L);
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(dyn));
        }

        [Benchmark]
        public object? Dyn_RoundTrip_Float()
        {
            var dyn = HashlinkMarshal.GetDyn(3.5f);
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(dyn));
        }

        [Benchmark]
        public object? Dyn_RoundTrip_Double()
        {
            var dyn = HashlinkMarshal.GetDyn(3.5d);
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(dyn));
        }

        [Benchmark]
        public object? Dyn_RoundTrip_NInt()
        {
            var dyn = HashlinkMarshal.GetDyn((nint)42);
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(dyn));
        }

        // ---------------------------------------------------------------------
        // Dynamic value pool boundaries
        // ---------------------------------------------------------------------

        [Benchmark]
        public nint Dyn_Int_Hit_42()
        {
            return HashlinkMarshal.GetDyn(42);
        }

        [Benchmark]
        public nint Dyn_Int_Miss_114514()
        {
            return HashlinkMarshal.GetDyn(114514);
        }

        [Benchmark]
        public nint Dyn_Int_Lower_Hit()
        {
            return HashlinkMarshal.GetDyn(-128);
        }

        [Benchmark]
        public nint Dyn_Int_Upper_Hit()
        {
            return HashlinkMarshal.GetDyn(128);
        }

        [Benchmark]
        public nint Dyn_Int_Below_Miss()
        {
            return HashlinkMarshal.GetDyn(-129);
        }

        [Benchmark]
        public nint Dyn_Int_Above_Miss()
        {
            return HashlinkMarshal.GetDyn(129);
        }

        [Benchmark]
        public nint Dyn_UShort_Hit()
        {
            return HashlinkMarshal.GetDyn((ushort)256);
        }

        [Benchmark]
        public nint Dyn_UShort_Miss()
        {
            return HashlinkMarshal.GetDyn((ushort)257);
        }

        [Benchmark]
        public nint Dyn_Byte_Hit()
        {
            return HashlinkMarshal.GetDyn((byte)255);
        }

        [Benchmark]
        public nint Dyn_Bool_Hit()
        {
            return HashlinkMarshal.GetDyn(true);
        }

        // ---------------------------------------------------------------------
        // Type resolution cache
        // ---------------------------------------------------------------------

        [Benchmark]
        public HashlinkType? Type_Cached()
        {
            return HashlinkMarshal.GetHashlinkType(typeof(double));
        }

        [Benchmark]
        public HashlinkType? Type_NoCache()
        {
            return marshaler.GetHashlinkTypeNoCache(typeof(double));
        }

        [Benchmark]
        public HashlinkType? Type_Delegate_Cached()
        {
            return HashlinkMarshal.GetHashlinkType(typeof(Action));
        }

        [Benchmark]
        public HashlinkType? Type_Proxy_Cached()
        {
            return HashlinkMarshal.GetHashlinkType(typeof(Point));
        }

        [Benchmark]
        public HashlinkType? Type_Proxy_NoCache()
        {
            return marshaler.GetHashlinkTypeNoCache(typeof(Point));
        }

        // ---------------------------------------------------------------------
        // Field access: string name vs hashed name vs cached offset
        // ---------------------------------------------------------------------

        [Benchmark]
        public object? Field_String_Get()
        {
            return rawPoint.GetFieldValue("x");
        }

        [Benchmark]
        public object? Field_Hash_Get()
        {
            return rawPoint.GetFieldValue(fieldHash);
        }

        [Benchmark]
        public double Field_CachedOffset_Get()
        {
            return *(double*)(rawPoint.HashlinkPointer + fieldOffset);
        }

        [Benchmark]
        public void Field_String_Set()
        {
            rawPoint.SetFieldValue("x", 3.5d);
        }

        [Benchmark]
        public void Field_Hash_Set()
        {
            rawPoint.SetFieldValue(fieldHash, 3.5d);
        }

        [Benchmark]
        public void Field_CachedOffset_Set()
        {
            *(double*)(rawPoint.HashlinkPointer + fieldOffset) = 3.5d;
        }

        // ---------------------------------------------------------------------
        // String cache
        // ---------------------------------------------------------------------

        [Benchmark]
        public void String_New()
        {
            _ = new HashlinkString(internedText);
        }

        [Benchmark]
        public void String_AsHaxeString_Interned()
        {
            _ = internedText.AsHaxeString();
        }

        [Benchmark]
        public void String_AsHaxeString_NonInterned()
        {
            _ = nonInternedText.AsHaxeString();
        }
    }
}
