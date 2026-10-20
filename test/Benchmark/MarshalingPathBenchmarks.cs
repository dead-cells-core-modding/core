using BenchmarkDotNet.Attributes;
using dc.h2d.col;
using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime;
using ModCore.Utilities;

namespace DCCMBenchmark
{
    /// <summary>
    /// Isolates the individual cost points on the Hashlink &lt;-&gt; CoreCLR
    /// marshaling path. Every benchmark changes one variable only: thread
    /// registration, type resolution, primitive write/read, dynamic pooling,
    /// string creation, object-handle conversion, proxy access, arrays and
    /// callbacks.
    /// <para>
    /// Run with <c>--filter "*MarshalingPathBenchmarks*" --anyCategories Smoke --quick</c>
    /// for a fast smoke check, or <c>--fast</c> for the full set.
    /// </para>
    /// </summary>
    [MemoryDiagnoser]
    public unsafe class MarshalingPathBenchmarks
    {
        private const int IntValue = 42;
        private const int IntMiss = 114514;
        private const double DoubleValue = 3.5;

        private HashlinkType i32Type = null!;
        private HashlinkType f64Type = null!;
        private HashlinkType dynType = null!;
        private HashlinkObjectType pointType = null!;
        private DefaultHashlinkMarshaler marshaler = null!;
        private Type intClrType = typeof(int);
        private Type pointClrType = typeof(Point);

        private string shortText = null!;
        private string text = null!;
        private HashlinkString hashlinkText = null!;
        private HashlinkObject rawPoint = null!;
        private Point proxyPoint = null!;
        private HashlinkArray intArray = null!;
        private HashlinkDynObj dynObj = null!;

        private HashlinkClosure normalizeClosure = null!;
        private Action normalizeDelegate = null!;

        private static HashlinkClosure? managedCallbackOwner;
        private static HashlinkClosure? managedCallback;
        private static Action? managedCallbackDelegate;
        private static int callbackCounter;

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            var known = HashlinkMarshal.Module.KnownTypes;
            i32Type = known.I32;
            f64Type = known.F64;
            dynType = known.Dynamic;
            marshaler = (DefaultHashlinkMarshaler)HashlinkMarshal.DefaultMarshaler;

            pointType = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point")!;

            shortText = "x";
            text = "The quick brown fox jumps over the lazy dog. 0123456789";
            hashlinkText = new HashlinkString(text);

            rawPoint = (HashlinkObject)pointType.CreateInstance();
            rawPoint.SetFieldValue("x", 3d);
            rawPoint.SetFieldValue("y", 4d);

            double x = 3;
            double y = 4;
            proxyPoint = new Point(new(ref x), new(ref y));

            intArray = new HashlinkArray(i32Type, 16);
            for (var i = 0; i < intArray.Count; i++)
            {
                intArray[i] = i;
            }

            dynObj = new HashlinkDynObj();
            dynObj.SetFieldValue("value", IntValue);

            normalizeClosure = (HashlinkClosure)rawPoint.GetFieldValue("normalize")!;
            normalizeDelegate = normalizeClosure.CreateDelegate<Action>();

            if (managedCallbackOwner == null)
            {
                var funcType = (HashlinkFuncType)HashlinkMarshal.GetHashlinkType(typeof(Action))!;
                managedCallbackOwner = new HashlinkClosure(funcType, (Action)(() => callbackCounter++));
                managedCallback = new HashlinkClosure(
                    HashlinkObjPtr.Get(managedCallbackOwner.HashlinkPointer));
                managedCallbackDelegate = managedCallback.CreateDelegate<Action>();
            }

            // Warm the identity cache once so the cache-hit benchmark only
            // pays the lookup, not the first HashlinkString/proxy creation.
            _ = text.AsHaxeString();
            _ = shortText.AsHaxeString();
        }

        // ---------------------------------------------------------------------
        // 1. Boundary / thread registration
        // ---------------------------------------------------------------------

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public bool Boundary_EnsureThreadRegistered()
        {
            return HashlinkMarshal.EnsureThreadRegistered();
        }

        // ---------------------------------------------------------------------
        // 2. Write path: known type, unknown type, dynamic, pointer
        // ---------------------------------------------------------------------

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public void Write_Int_KnownType()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, IntValue, i32Type);
        }

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public void Write_Int_UnspecifiedType()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, IntValue, null);
        }

        [Benchmark]
        public void Write_Int_DynamicType()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, IntValue, dynType);
        }

        [Benchmark]
        public void Write_Double_KnownType()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, DoubleValue, f64Type);
        }

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public void Write_ObjectPointer()
        {
            nint buffer = 0;
            HashlinkMarshal.WriteData(&buffer, rawPoint, null);
        }

        // ---------------------------------------------------------------------
        // 3. Read path
        // ---------------------------------------------------------------------

        [Benchmark]
        public object? Read_Int_KnownType()
        {
            int buffer = IntValue;
            return HashlinkMarshal.ReadData(&buffer, i32Type);
        }

        [Benchmark]
        public object? Read_Double_KnownType()
        {
            double buffer = DoubleValue;
            return HashlinkMarshal.ReadData(&buffer, f64Type);
        }

        [Benchmark]
        public object? Read_ObjectPointer()
        {
            nint buffer = rawPoint.HashlinkPointer;
            return HashlinkMarshal.ReadData(&buffer, pointType);
        }

        // ---------------------------------------------------------------------
        // 4. Type resolution
        // ---------------------------------------------------------------------

        [Benchmark]
        public HashlinkType? Type_Cached_Int()
        {
            return HashlinkMarshal.GetHashlinkType(intClrType);
        }

        [Benchmark]
        public HashlinkType? Type_Cached_Point()
        {
            return HashlinkMarshal.GetHashlinkType(pointClrType);
        }

        [Benchmark]
        public HashlinkType? Type_NoCache_Int()
        {
            return marshaler.GetHashlinkTypeNoCache(intClrType);
        }

        // ---------------------------------------------------------------------
        // 5. Dynamic values: pool hit vs allocation
        // ---------------------------------------------------------------------

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public nint Dynamic_Int_Hit()
        {
            return HashlinkMarshal.GetDyn(IntValue);
        }

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public nint Dynamic_Int_Miss()
        {
            return HashlinkMarshal.GetDyn(IntMiss);
        }

        [Benchmark]
        public nint Dynamic_Long()
        {
            return HashlinkMarshal.GetDyn((long)IntMiss);
        }

        [Benchmark]
        public nint Dynamic_Double()
        {
            return HashlinkMarshal.GetDyn(DoubleValue);
        }

        [Benchmark]
        public nint Dynamic_NInt()
        {
            return HashlinkMarshal.GetDyn((nint)IntMiss);
        }

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public nint Dynamic_String()
        {
            return HashlinkMarshal.GetDyn(text);
        }

        // ---------------------------------------------------------------------
        // 6. Strings: allocation, read, identity cache, proxy
        // ---------------------------------------------------------------------

        [Benchmark]
        public void String_New_Short()
        {
            _ = new HashlinkString(shortText);
        }

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public void String_New_Long()
        {
            _ = new HashlinkString(text);
        }

        [Benchmark]
        public string? String_Read_Cached()
        {
            return hashlinkText.TypedValue;
        }

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public dc.String String_AsHaxeString_CacheHit()
        {
            return text.AsHaxeString();
        }

        [Benchmark]
        public dc.String String_AsHaxeString_NewReference()
        {
            return new string(text.AsSpan()).AsHaxeString();
        }

        [Benchmark]
        public dc.String String_Proxy_Create()
        {
            return new HashlinkString(text).AsHaxe<dc.String>();
        }

        // ---------------------------------------------------------------------
        // 7. Object handles / proxy conversion
        // ---------------------------------------------------------------------

        [Benchmark]
        public HashlinkObjPtr Object_HandleGet()
        {
            return HashlinkObjPtr.Get(rawPoint.HashlinkPointer);
        }

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public object? Object_ConvertCached()
        {
            return HashlinkMarshal.ConvertHashlinkObject(
                HashlinkObjPtr.Get(rawPoint.HashlinkPointer));
        }

        [Benchmark]
        public HashlinkObject Object_Create()
        {
            return (HashlinkObject)pointType.CreateInstance();
        }

        // ---------------------------------------------------------------------
        // 8. HaxeProxy direct access
        // ---------------------------------------------------------------------

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public double Proxy_GetDouble()
        {
            return proxyPoint.x;
        }

        [Benchmark]
        public void Proxy_SetDouble()
        {
            proxyPoint.x = 3.5;
        }

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public void Proxy_CallNormalize()
        {
            proxyPoint.normalize();
        }

        // ---------------------------------------------------------------------
        // 9. Arrays and dynamic objects
        // ---------------------------------------------------------------------

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public void Array_SetInt()
        {
            intArray[0] = IntValue;
        }

        [Benchmark]
        public object? Array_GetInt()
        {
            return intArray[0];
        }

        [Benchmark]
        public void DynObj_SetField()
        {
            dynObj.SetFieldValue("value", IntValue);
        }

        // ---------------------------------------------------------------------
        // 10. Closures / callbacks
        // ---------------------------------------------------------------------

        [Benchmark]
        public void Closure_DelegateInvoke()
        {
            normalizeDelegate();
        }

        [Benchmark]
        public void Closure_DynamicInvoke()
        {
            normalizeClosure.DynamicInvoke();
        }

        [Benchmark]
        [BenchmarkCategory("Smoke")]
        public void Callback_DelegateInvoke()
        {
            managedCallbackDelegate!();
        }

        [Benchmark]
        public void Callback_DynamicInvoke()
        {
            managedCallback!.DynamicInvoke();
        }
    }
}
