using BenchmarkDotNet.Attributes;
using dc.h2d.col;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;

namespace DCCMBenchmark
{
    /// <summary>
    /// Object marshaling between managed .NET values and Hashlink values,
    /// covering strings, dynamic values, field access, arrays and Haxe proxies.
    /// </summary>
    [MemoryDiagnoser]
    public unsafe class MarshalBenchmarks
    {
        private const int IntValue = 114514;
        private const double DoubleValue = 114514.0;

        private string text = null!;
        private HashlinkString hashlinkText = null!;
        private HashlinkDynObj dynObj = null!;
        private HashlinkArray intArray = null!;
        private nint intDyn;
        private nint stringDyn;
        private HashlinkObject rawPoint = null!;
        private Point proxyPoint = null!;

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            text = "The quick brown fox jumps over the lazy dog. 0123456789";

            hashlinkText = new HashlinkString(text);
            dynObj = new HashlinkDynObj();
            intDyn = HashlinkMarshal.GetDyn(IntValue);
            stringDyn = HashlinkMarshal.GetDyn(text);

            intArray = new HashlinkArray(HashlinkMarshal.Module.KnownTypes.I32, 16);
            for (var i = 0; i < intArray.Count; i++)
            {
                intArray[i] = i;
            }

            rawPoint = (HashlinkObject)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point").CreateInstance();
            rawPoint.SetFieldValue("x", 3d);
            rawPoint.SetFieldValue("y", 4d);

            double x = 3;
            double y = 4;
            proxyPoint = new Point(new(ref x), new(ref y));
        }

        // ---------------------------------------------------------------------
        // Managed -> Hashlink
        // ---------------------------------------------------------------------

        [Benchmark]
        public void String_ManagedToHashlink()
        {
            _ = new HashlinkString(text);
        }

        // OutOfMemory
        //[Benchmark]
        public void Dyn_IntToHashlink()
        {
            _ = HashlinkMarshal.GetDyn(IntValue);
        }

        [Benchmark]
        public void Dyn_StringToHashlink()
        {
            _ = HashlinkMarshal.GetDyn(text);
        }

        [Benchmark]
        public void Object_SetFieldDouble()
        {
            rawPoint.SetFieldValue("x", DoubleValue);
        }

        [Benchmark]
        public void Array_SetInt()
        {
            intArray[0] = IntValue;
        }

        [Benchmark]
        public void Proxy_SetDouble()
        {
            proxyPoint.x = DoubleValue;
        }

        // ---------------------------------------------------------------------
        // Hashlink -> Managed
        // ---------------------------------------------------------------------

        [Benchmark]
        public void String_HashlinkToManaged()
        {
            _ = hashlinkText.ToString();
        }

        [Benchmark]
        public void Dyn_IntToManaged()
        {
            _ = HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(intDyn));
        }

        [Benchmark]
        public void Dyn_StringToManaged()
        {
            _ = HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(stringDyn));
        }

        [Benchmark]
        public void Object_GetFieldDouble()
        {
            _ = rawPoint.GetFieldValue("x");
        }

        [Benchmark]
        public void Array_GetInt()
        {
            _ = intArray[0];
        }

        [Benchmark]
        public void Proxy_GetDouble()
        {
            _ = proxyPoint.x;
        }

        // ---------------------------------------------------------------------
        // Allocation / wrapper creation
        // ---------------------------------------------------------------------

        [Benchmark]
        public void Object_CreateInstance()
        {
            _ = HashlinkMarshal.Module.GetTypeByName("h2d.col.Point").CreateInstance();
        }

        [Benchmark]
        public void Object_ConvertCachedHandle()
        {
            _ = HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(rawPoint.HashlinkPointer));
        }

        [Benchmark]
        public void Proxy_Create()
        {
            double x = 3;
            double y = 4;
            _ = new Point(new(ref x), new(ref y));
        }

        [Benchmark]
        public void DynObj_SetField()
        {
            dynObj.SetFieldValue("value", IntValue);
        }
    }
}
