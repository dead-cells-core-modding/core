using Hashlink;
using ModCore.Hashlink;
using ModCore.Modules.Events;
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

        public nint MainWindowPtr { get; private set; }

        private static object? Hook_Boot_update(HashlinkFunc orig, HashlinkObject self, double dt)
        {

            return orig.Call((float)dt, (nint) self.HashlinkValue->val.ptr);
        }

        void IOnBeforeGameStartup.OnBeforeGameStartup()
        {
            hhook.CreateHook(HashlinkUtils.FindFunction("Boot", "update"), Hook_Boot_update);
        }

        void IOnModCoreInjected.OnModCoreInjected()
        {
            hhook = HashlinkHook.Instance;

        }
    }
}
