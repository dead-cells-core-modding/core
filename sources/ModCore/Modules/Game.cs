using Hashlink;
using ModCore.Events;
using ModCore.Hashlink;
using ModCore.Track;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static SDL2.SDL;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class Game : CoreModule<Game>, IOnModCoreInjected, IOnBeforeGameStartup
    {
        private HashlinkHook hhook = null!;
        public override int Priority => ModulePriorities.Game;

        public int GameVersion { get; private set; }
        public nint MainWindowPtr { get; private set; }

        private object? Hook_Boot_update(HashlinkFunc orig, HashlinkObject self, double dt)
        {
            EventSystem.BroadcastEvent<IOnFrameUpdate, double>(dt);
            return orig.Call(self, dt);
        }
        private object? Hook_Boot_Init(HashlinkFunc orig, HashlinkObject self)
        {
            EventSystem.BroadcastEvent<IOnGameInit>();
            return orig.Call(self);
        }
        private object? Hook_Boot_endInit(HashlinkFunc orig, HashlinkObject self)
        {
            MainWindowPtr = (nint)self.Dynamic.engine.window.window.win;

            Logger.Information("Game initialization completed");
            
            SDL_SetWindowTitle(MainWindowPtr, "Dead Cells with Core Modding");

            EventSystem.BroadcastEvent<IOnGameEndInit>();

            return orig.Call(self);
        }
        void IOnBeforeGameStartup.OnBeforeGameStartup()
        {
            hhook.CreateHook(HashlinkUtils.FindFunction("Boot", "update"), Hook_Boot_update);
            hhook.CreateHook(HashlinkUtils.FindFunction("Boot", "endInit"), Hook_Boot_endInit);
            hhook.CreateHook(HashlinkUtils.FindFunction("Boot", "init"), Hook_Boot_endInit);
        }

        void IOnModCoreInjected.OnModCoreInjected()
        {
            hhook = HashlinkHook.Instance;

        }
    }
}
