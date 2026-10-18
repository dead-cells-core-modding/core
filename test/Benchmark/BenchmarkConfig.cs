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
                job = Job.Dry.WithToolchain(InProcessEmitToolchain.Instance);
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
