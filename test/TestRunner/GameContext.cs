using Hashlink;
using Hashlink.Marshaling;
using ModCore;
using ModCore.Events;
using ModCore.Events.Interfaces.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TestRunner;

namespace TestRunner
{
    public class GameContext : IEventReceiver, IOnBeforeGameInit
    {
        private readonly ManualResetEvent gameInitEvent = new(false);
        private readonly Thread gameThread;

        int IEventReceiver.Priority => 100000;

        private void GameThread()
        {
            Console.WriteLine("Setup Core Config");
            Core.Config.Value = new()
            {
                AllowCloseConsole = false,
                EnableGoldberg = true,
                SkipLogoSplash = true,
                GeneratePseudocodeAssembly = true,
                NoConsole = !Debugger.IsAttached,
            };
            ContextConfig.Config = ContextConfig.Default with
            {
                consoleOutput = Debugger.IsAttached
            };
            Console.WriteLine("Setup receiver");
            EventSystem.AddReceiver(this);
            Console.WriteLine("Start game");
            Startup.StartGame();
        }

        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            gameInitEvent.Set();
            while(true)
            {
                Thread.Yield();
            }
        }


        public GameContext()
        {
            Console.WriteLine("Setup enviroment variables");
            var testRoot = Path.Combine(
                     Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH")!,
                    "coremod",
                    "test");
            Environment.SetEnvironmentVariable("DCCM_OverridePath_CORE_ROOT",
                Path.Combine(
                    Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH")!,
                    "coremod"));
            Environment.SetEnvironmentVariable("DCCM_OverridePath_CORE_CONFIG",
                Path.Combine(testRoot,
                    "config"
                    ));
            Environment.SetEnvironmentVariable("DCCM_OverridePath_CORE_LOGS",
                 Path.Combine(testRoot,
                    "logs"
                    ));

            Console.WriteLine("Start game thread");
            gameThread = new Thread(GameThread)
            {
                Name = "Game Thread",
                IsBackground = true
            };
            gameThread.Start();

            Console.WriteLine("Waiting game init");
            gameInitEvent.WaitOne();
            Console.WriteLine("Test inited");
            HashlinkThread.RegisterThread();
        }
    }
}
