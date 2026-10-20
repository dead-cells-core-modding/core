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
                      --filter *FastMarshalingBenchmarks*   focused, fast marshaling checks
                      --filter *MarshalBenchmarks*          only marshaling benchmarks
                      --filter *InvocationBenchmarks*       only raw invocation benchmarks
                      --filter *HaxeProxyBenchmarks*        only HaxeProxy comparison benchmarks
                      --filter *MarshalingPathBenchmarks*   HL <-> CoreCLR marshaling cost points
                      --filter *ManagedObjectBenchmarks*    only the pure .NET control group
                      --fast                                short job (2 warmup + 5 iterations)
                      --quick                               smoke check (1 warmup + 1 iteration)
                      --quick --anyCategories Smoke         14 key marshaling checks (~2 min)

                    Do not use --job with this project: every job would need an
                    in-process toolchain to reuse the booted Hashlink VM.

                    Requires DEAD_CELLS_GAME_PATH and an installed DCCM MDK.
                    """);
                return;
            }

            var artifactsPath = Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts");
            var quick = args.Contains("--quick");
            var fast = args.Contains("--fast");

            if (quick && fast)
            {
                Console.Error.WriteLine("--quick and --fast are mutually exclusive.");
                return;
            }

            if (quick || fast)
            {
                args = args.Where(x => x != "--quick" && x != "--fast").ToArray();
            }

            GameBootstrap.EnsureInitialized();

            var config = BenchmarkConfig.Create(artifactsPath, quick, fast);
            var summaries = BenchmarkRunner.Run(typeof(Program).Assembly, config, args);

            var failed = summaries.Any(s => s.HasCriticalValidationErrors);
            Environment.Exit(failed ? 1 : 0);
        }
    }
}
