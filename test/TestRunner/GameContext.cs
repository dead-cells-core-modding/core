using Hashlink;
using ModCore;
using ModCore.Events;
using ModCore.Events.Interfaces.Game;
using ModCore.Modules;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TestRunner
{
    public class GameContext : IEventReceiver, IOnBeforeGameInit
    {
        private readonly ManualResetEvent gameInitEvent = new(false);
        private readonly Thread gameThread;

        int IEventReceiver.Priority => 100000;

        private void GameThread()
        {
            //Console.WriteLine("Setup Core Config");
            Core.Config.Value = new()
            {
                AllowCloseConsole = false,
                EnableGoldberg = true,
                SkipLogoSplash = true,
                GeneratePseudocodeAssembly = true
            };
            ContextConfig.Config = ContextConfig.Default with
            {
                consoleOutput = Debugger.IsAttached || Environment.GetEnvironmentVariable("DCCM_TEST_OUTPUT") == "1",
                disableWorkerProcessUtils = true,
                suppressFatalWindows = true,
            };
            //Console.WriteLine("Setup receiver");
            EventSystem.AddReceiver(this);
            //Console.WriteLine("Start game");
            Startup.StartGame();
        }

        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            gameInitEvent.Set();
            while (true)
            {
                Thread.Yield();
            }
        }


        public GameContext()
        {
            //Console.WriteLine("Setup enviroment variables");
            var gamePath = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH");
            if (string.IsNullOrEmpty(gamePath))
            {
                throw new InvalidOperationException(
                    "DEAD_CELLS_GAME_PATH environment variable is not set. " +
                    "Please set it to the Dead Cells game installation directory, e.g.:\n" +
                    "  export DEAD_CELLS_GAME_PATH=\"path to Dead Cells\"");
            }
            var testRoot = Path.Combine(gamePath, "coremod", "test");
            Environment.SetEnvironmentVariable("DCCM_OverridePath_CORE_ROOT",
                Path.Combine(gamePath, "coremod"));
            Environment.SetEnvironmentVariable("DCCM_OverridePath_CORE_CONFIG",
                Path.Combine(testRoot, "config"));
            Environment.SetEnvironmentVariable("DCCM_OverridePath_CORE_LOGS",
                Path.Combine(testRoot, "logs"));

            //Console.WriteLine("Start game thread");
            gameThread = new Thread(GameThread)
            {
                Name = "Game Thread",
                IsBackground = true
            };

            gameThread.Start();
            gameInitEvent.WaitOne();
            HashlinkThread.RegisterThread();
        }
    }
}
