using BenchmarkDotNet.Running;

namespace DCCMBenchmark
{
    /// <summary>
    /// Entry point for the DCCM benchmark suite.
    /// <para>
    /// The Dead Cells Hashlink VM is booted exactly once, then BenchmarkDotNet
    /// runs all benchmarks in-process because the VM cannot be initialized
    /// twice and the whole game was never designed to be started headlessly.
    /// </para>
    /// <para>
    /// Usage: <c>dotnet run -c Release -- [BenchmarkDotNet arguments]</c>.
    /// </para>
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Any(x => x is "--help" or "-h" or "-?"))
            {
                Console.WriteLine(
                    """
                    DCCM benchmarks (object marshaling, mutual invocation, HaxeProxy calls).

                      dotnet run -c Release -- [BenchmarkDotNet arguments]

                    Common arguments:
                      --filter *MarshalBenchmarks*          only marshaling benchmarks
                      --filter *InvocationBenchmarks*       only raw invocation benchmarks
                      --filter *HaxeProxyBenchmarks*        only HaxeProxy comparison benchmarks
                      --filter *ManagedObjectBenchmarks*    only the pure .NET control group
                      --quick                               single quick iteration

                    Do not use --job with this project: every job would need an
                    in-process toolchain to reuse the booted Hashlink VM.

                    Requires DEAD_CELLS_GAME_PATH and an installed DCCM MDK.
                    """);
                return;
            }

            var artifactsPath = Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts");
            var quick = args.Contains("--quick");
            if (quick)
            {
                args = args.Where(x => x != "--quick").ToArray();
            }

            GameBootstrap.EnsureInitialized();

            var config = BenchmarkConfig.Create(artifactsPath, quick);
            var summaries = BenchmarkRunner.Run(typeof(Program).Assembly, config, args);

            var failed = summaries.Any(s => s.HasCriticalValidationErrors);
            Environment.Exit(failed ? 1 : 0);
        }
    }
}
