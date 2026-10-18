using BenchmarkDotNet.Attributes;

namespace DCCMBenchmark
{
    /// <summary>
    /// Pure .NET object/field access control group. It mirrors the operations
    /// measured by <see cref="HaxeProxyBenchmarks"/> on a plain managed type,
    /// so the cost of crossing the Hashlink boundary can be separated from
    /// the cost of the operation itself.
    /// </summary>
    [MemoryDiagnoser]
    public class ManagedObjectBenchmarks
    {
        private ManagedPoint point = null!;

        [GlobalSetup]
        public void Setup()
        {
            point = new ManagedPoint(3, 4);
        }

        [Benchmark(Baseline = true)]
        public double Field_Get()
        {
            return point.x;
        }

        [Benchmark]
        public void Field_Set()
        {
            point.x = 3.5;
        }

        [Benchmark]
        public void Method_Call()
        {
            point.Normalize();
        }

        [Benchmark]
        public ManagedPoint Create()
        {
            return new ManagedPoint(3, 4);
        }

        public sealed class ManagedPoint
        {
            public double x;
            public double y;

            public ManagedPoint(double x, double y)
            {
                this.x = x;
                this.y = y;
            }

            public void Normalize()
            {
                var length = Math.Sqrt((x * x) + (y * y));
                if (length != 0)
                {
                    x /= length;
                    y /= length;
                }
            }
        }
    }
}
