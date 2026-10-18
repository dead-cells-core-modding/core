using BenchmarkDotNet.Attributes;
using dc.h2d.col;
using ModCore.Modules;

namespace DCCMBenchmark
{
    internal sealed class HookBenchmarkHandler
    {
        public bool CallOrig { get; init; }

        public void Invoke(Hook_Point.orig_normalize orig, Point self)
        {
            if (CallOrig)
            {
                orig(self);
            }
        }
    }

    /// <summary>
    /// Hook invocation overhead. HookCount controls the chain length and
    /// CallOrig controls whether each hook forwards to the original function.
    /// Run with --fast for a quick local check.
    /// </summary>
    [MemoryDiagnoser]
    public class HookBenchmarks
    {
        [Params(0, 1, 2, 4)]
        public int HookCount;

        [Params(true, false)]
        public bool CallOrig;

        private Point point = null!;
        private readonly List<Hook_Point.hook_normalize> hooks = [];
        private readonly List<HookBenchmarkHandler> handlers = [];

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            double x = 3;
            double y = 4;
            point = new Point(new(ref x), new(ref y));

            for (var i = 0; i < HookCount; i++)
            {
                var handler = new HookBenchmarkHandler
                {
                    CallOrig = CallOrig,
                };
                var hook = new Hook_Point.hook_normalize(handler.Invoke);
                Hook_Point.normalize += hook;
                handlers.Add(handler);
                hooks.Add(hook);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            foreach (var hook in hooks)
            {
                Hook_Point.normalize -= hook;
            }
            hooks.Clear();
            handlers.Clear();
        }

        [Benchmark]
        public void Proxy_Normalize()
        {
            point.normalize();
        }
    }

    /// <summary>
    /// Cost of enabling and disabling an existing hook. The hook is created
    /// once in setup; each benchmark operation toggles it on and off.
    /// </summary>
    [MemoryDiagnoser]
    public class HookRegistrationBenchmarks
    {
        private HashlinkHooks.HookHandle handle = null!;
        private HookBenchmarkHandler handler = null!;

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            handler = new HookBenchmarkHandler
            {
                CallOrig = true,
            };
            var hook = new Hook_Point.hook_normalize(handler.Invoke);
            handle = HashlinkHooks.Instance.CreateHook(
                "h2d.col.Point",
                "normalize",
                hook,
                enableByDefault: false);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            handle.Disable();
        }

        [Benchmark]
        public void EnableDisable()
        {
            handle.Enable();
            handle.Disable();
        }
    }
}
