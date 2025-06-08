using ModCore;
using ModCore.Events;
using ModCore.Events.Interfaces.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            Core.Config.Value = new()
            {
                AllowCloseConsole = false,
                EnableGoldberg = true,
                SkipLogoSplash = true,
                GeneratePseudocodeAssembly = true,
                NoConsole = !Debugger.IsAttached,
            };
            EventSystem.AddReceiver(this);
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
            gameThread = new Thread(GameThread)
            {
                Name = "Game Thread",
                IsBackground = true
            };
            gameThread.Start();
            gameInitEvent.WaitOne();
        }
    }
}
