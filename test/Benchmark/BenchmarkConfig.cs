using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace DCCMBenchmark
{
    internal static class BenchmarkConfig
    {
        public static IConfig Create(string artifactsPath, bool quick = false, bool fast = false)
        {
            Job job;
            string id;

            if (quick)
            {
                // Let BDN run its pilot so the benchmark methods reach tier-1
                // code; the default fixed invocation count would keep them in
                // tier-0 and report 10x+ inflated numbers.
                job = Job.Default
                    .WithToolchain(InProcessEmitToolchain.Instance)
                    .WithWarmupCount(1)
                    .WithIterationCount(1)
                    .WithLaunchCount(1);
                id = "InProcessQuick";
            }
            else if (fast)
            {
                job = Job.Default
                    .WithToolchain(InProcessEmitToolchain.Instance)
                    .WithWarmupCount(2)
                    .WithIterationCount(5)
                    .WithLaunchCount(1);
                id = "InProcessFast";
            }
            else
            {
                job = Job.Default.WithToolchain(InProcessEmitToolchain.Instance);
                id = "InProcess";
            }

            return ManualConfig.CreateMinimumViable()
                // The Hashlink VM can only be initialized once per process, so
                // every benchmark has to share the process that booted the game.
                .AddJob(job.WithId(id))
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddExporter(MarkdownExporter.GitHub)
                .WithArtifactsPath(artifactsPath);
        }
    }
}
