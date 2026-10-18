using BenchmarkDotNet.Attributes;
using dc.h2d.col;
using dc.hl.types;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;

namespace DCCMBenchmark
{
    /// <summary>
    /// HaxeProxy/GameProxy call comparison. Every proxy operation is paired
    /// with the equivalent raw HashlinkSharp call (reflection field access,
    /// closure invocation, dynamic access or HashlinkArray access).
    /// </summary>
    [MemoryDiagnoser]
    public unsafe class HaxeProxyBenchmarks
    {
        private HashlinkObjectType pointType = null!;
        private Point proxyPoint = null!;
        private HashlinkObject rawPoint = null!;
        private HashlinkClosure normalizeClosure = null!;
        private Action normalizeDelegate = null!;
        private dynamic dynamicPoint = null!;

        private ArrayObj proxyArray = null!;
        private HashlinkArray rawArray = null!;

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            pointType = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point")!;

            double x = 3;
            double y = 4;
            proxyPoint = new Point(new(ref x), new(ref y));

            rawPoint = CreateRawPoint(3, 4);
            dynamicPoint = rawPoint.AsDynamic();

            normalizeClosure = (HashlinkClosure)rawPoint.GetFieldValue("normalize")!;
            normalizeDelegate = normalizeClosure.CreateDelegate<Action>();

            rawArray = new HashlinkArray(HashlinkMarshal.Module.KnownTypes.Dynamic, 4);
            for (var i = 0; i < rawArray.Count; i++)
            {
                rawArray[i] = i * 10;
            }
            proxyArray = new ArrayObj
            {
                array = rawArray,
            };
        }

        private HashlinkObject CreateRawPoint(double x, double y)
        {
            var point = (HashlinkObject)pointType.CreateInstance();
            point.SetFieldValue("x", x);
            point.SetFieldValue("y", y);
            return point;
        }

        // ---------------------------------------------------------------------
        // Object creation
        // ---------------------------------------------------------------------

        [Benchmark(Baseline = true)]
        public void Proxy_CreatePoint()
        {
            double x = 3;
            double y = 4;
            _ = new Point(new(ref x), new(ref y));
        }

        [Benchmark]
        public void Raw_CreatePoint()
        {
            _ = CreateRawPoint(3, 4);
        }

        // ---------------------------------------------------------------------
        // Field access
        // ---------------------------------------------------------------------

        [Benchmark]
        public void Proxy_GetField()
        {
            _ = proxyPoint.x;
        }

        [Benchmark]
        public void Raw_GetField()
        {
            _ = rawPoint.GetFieldValue("x");
        }

        [Benchmark]
        public void Proxy_SetField()
        {
            proxyPoint.x = 3.5;
        }

        [Benchmark]
        public void Raw_SetField()
        {
            rawPoint.SetFieldValue("x", 3.5);
        }

        // ---------------------------------------------------------------------
        // Void method invocation
        // ---------------------------------------------------------------------

        [Benchmark]
        public void Proxy_Normalize()
        {
            proxyPoint.normalize();
        }

        [Benchmark]
        public void Raw_ClosureDynamicInvoke_Normalize()
        {
            normalizeClosure.DynamicInvoke();
        }

        [Benchmark]
        public void Raw_ClosureDelegateInvoke_Normalize()
        {
            normalizeDelegate();
        }

        [Benchmark]
        public void Dynamic_Normalize()
        {
            dynamicPoint.normalize();
        }

        // ---------------------------------------------------------------------
        // Method invocation with arguments and return value
        // ---------------------------------------------------------------------

        [Benchmark]
        public void Proxy_ArrayGetDyn()
        {
            _ = proxyArray.getDyn(0);
        }

        [Benchmark]
        public void Raw_ArrayIndexGet()
        {
            _ = rawArray[0];
        }

        [Benchmark]
        public void Proxy_ArraySetDyn()
        {
            proxyArray.setDyn(0, 42);
        }

        [Benchmark]
        public void Raw_ArrayIndexSet()
        {
            rawArray[0] = 42;
        }
    }
}
