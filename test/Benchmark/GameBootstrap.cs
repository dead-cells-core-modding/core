using Hashlink.Marshaling;
using ModCore;
using ModCore.Events;
using ModCore.Events.Interfaces.Game;
using ModCore.Modules;
using System.Diagnostics;

namespace DCCMBenchmark
{
    /// <summary>
    /// Boots the Dead Cells Hashlink VM once per process, mirroring
    /// <c>TestRunner.GameContext</c> from the integration tests.
    /// </summary>
    internal static class GameBootstrap
    {
        private static readonly object syncRoot = new();
        private static volatile bool initialized;

        private sealed class GameThreadBlocker : IEventReceiver, IOnBeforeGameInit
        {
            public ManualResetEventSlim Ready { get; } = new(false);
            private readonly ManualResetEventSlim stop = new(false);

            int IEventReceiver.Priority => 100000;

            void IOnBeforeGameInit.OnBeforeGameInit()
            {
                Ready.Set();

                // The game thread is already flagged as GC-blocking by the
                // Haxe -> .NET callback thunk, so parking it here keeps the
                // VM usable without burning a core while benchmarks run.
                stop.Wait();
            }
        }

        /// <summary>
        /// Returns when the game is initialized and the calling thread is
        /// registered with the Hashlink VM. Safe to call from any thread.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (initialized)
            {
                HashlinkMarshal.EnsureThreadRegistered();
                return;
            }

            lock (syncRoot)
            {
                if (initialized)
                {
                    HashlinkMarshal.EnsureThreadRegistered();
                    return;
                }

                Boot();

                initialized = true;
            }

            HashlinkMarshal.EnsureThreadRegistered();
        }

        private static void Boot()
        {
            var gamePath = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH");
            if (string.IsNullOrEmpty(gamePath))
            {
                throw new InvalidOperationException(
                    "DEAD_CELLS_GAME_PATH environment variable is not set. " +
                    "Set it to the Dead Cells installation directory, e.g.:\n" +
                    "  $env:DEAD_CELLS_GAME_PATH = \"E:\\SteamLibrary\\steamapps\\common\\Dead Cells\"");
            }

            var coreRoot = Path.Combine(gamePath, "coremod");
            var benchmarkRoot = Path.Combine(coreRoot, "benchmark");
            var modsRoot = Path.Combine(benchmarkRoot, "mods");
            var pluginsRoot = Path.Combine(benchmarkRoot, "plugins");
            Directory.CreateDirectory(modsRoot);
            Directory.CreateDirectory(pluginsRoot);

            Environment.SetEnvironmentVariable("DCCM_OverridePath_CORE_ROOT", coreRoot);
            Environment.SetEnvironmentVariable("DCCM_OverridePath_CORE_CONFIG", Path.Combine(benchmarkRoot, "config"));
            Environment.SetEnvironmentVariable("DCCM_OverridePath_CORE_LOGS", Path.Combine(benchmarkRoot, "logs"));
            // Isolate measurements from whichever mods the user has installed.
            Environment.SetEnvironmentVariable("DCCM_OverridePath_CORE_MODS", modsRoot);
            Environment.SetEnvironmentVariable("DCCM_OverridePath_CORE_PLUGINS", pluginsRoot);
            Environment.SetEnvironmentVariable("DCCM_EXTRA_MODS_PATHS", "");
            Environment.SetEnvironmentVariable("DCCM_DISABLE_REPORTER", "1");

            var consoleOutput = Debugger.IsAttached ||
                Environment.GetEnvironmentVariable("DCCM_BENCHMARK_OUTPUT") == "1";

            ContextConfig.Config = ContextConfig.Default with
            {
                consoleOutput = consoleOutput,
                disableWorkerProcessUtils = true,
                suppressFatalWindows = true,
            };

            Core.Config.Value = new CoreConfig
            {
                AllowCloseConsole = false,
                EnableGoldberg = true,
                SkipLogoSplash = true,
                GeneratePseudocodeAssembly = false,
            };

            var blocker = new GameThreadBlocker();
            EventSystem.AddReceiver(blocker);

            Exception? startupError = null;
            var gameThread = new Thread(() =>
            {
                try
                {
                    Startup.StartGame();
                }
                catch (Exception ex)
                {
                    startupError = ex;
                    blocker.Ready.Set();
                }
            })
            {
                Name = "Game Thread",
                IsBackground = true,
                Priority = ThreadPriority.Lowest,
            };

            gameThread.Start();
            blocker.Ready.Wait();

            if (startupError != null)
            {
                throw new InvalidOperationException("Failed to initialize the Dead Cells Hashlink VM.", startupError);
            }

            HashlinkMarshal.EnsureThreadRegistered();
        }
    }
}
