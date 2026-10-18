using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace DCCMBenchmark
{
    internal static class BenchmarkConfig
    {
        public static IConfig Create(string artifactsPath, bool quick = false)
        {
            var job = quick
                ? Job.Dry.WithToolchain(InProcessEmitToolchain.Instance)
                : Job.Default.WithToolchain(InProcessEmitToolchain.Instance);

            return ManualConfig.CreateMinimumViable()
                // The Hashlink VM can only be initialized once per process, so
                // every benchmark has to share the process that booted the game.
                .AddJob(job.WithId(quick ? "InProcessQuick" : "InProcess"))
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddExporter(MarkdownExporter.GitHub)
                .WithArtifactsPath(artifactsPath);
        }
    }
}
