using BenchmarkDotNet.Attributes;
using dc.h2d.col;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using HaxeString = dc.String;

namespace DCCMBenchmark
{
    /// <summary>
    /// Isolates HaxeProxy allocation cost:
    /// raw HashlinkString creation, fresh dc.String proxy creation, cached
    /// proxy lookup, interned string cache, and Point proxy creation.
    /// Run with --fast for a quick local measurement.
    /// </summary>
    [MemoryDiagnoser]
    public unsafe class ProxyAllocationBenchmarks
    {
        private string internedText = null!;
        private string nonInternedText = null!;
        private HashlinkString cachedString = null!;
        private HashlinkObjectType pointType = null!;

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            internedText = "benchmark.proxy.interned";
            nonInternedText = "benchmark.proxy.noninterned." + Guid.NewGuid().ToString("N");

            cachedString = new HashlinkString(nonInternedText);
            pointType = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point")!;

            // Warm up proxy type resolution / factories.
            _ = new HashlinkString("benchmark.proxy.warmup").AsHaxe<HaxeString>();
            double x = 0;
            double y = 0;
            _ = new Point(new(ref x), new(ref y));
        }

        // ---------------------------------------------------------------------
        // Strings
        // ---------------------------------------------------------------------

        [Benchmark(Baseline = true)]
        public HashlinkString String_New()
        {
            return new HashlinkString(nonInternedText);
        }

        [Benchmark]
        public HaxeString String_Proxy_Fresh()
        {
            return new HashlinkString(nonInternedText).AsHaxe<HaxeString>();
        }

        [Benchmark]
        public HaxeString String_Proxy_Cached()
        {
            return cachedString.AsHaxe<HaxeString>();
        }

        [Benchmark]
        public HaxeString String_AsHaxeString_Interned()
        {
            return internedText.AsHaxeString();
        }

        [Benchmark]
        public HaxeString String_AsHaxeString_NonInterned()
        {
            return nonInternedText.AsHaxeString();
        }

        // ---------------------------------------------------------------------
        // Point proxy vs raw object
        // ---------------------------------------------------------------------

        [Benchmark]
        public HashlinkObject RawPoint_Create()
        {
            return (HashlinkObject)pointType.CreateInstance();
        }

        [Benchmark]
        public Point ProxyPoint_Create()
        {
            double x = 3;
            double y = 4;
            return new Point(new(ref x), new(ref y));
        }
    }
}
