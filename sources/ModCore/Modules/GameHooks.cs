using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Patch;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Hashlink.Reflection.Types;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Events.Interfaces.VM;
using ModCore.Modules.AdvancedModules;
using System.Diagnostics;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class GameHooks : CoreModule<GameHooks>, IOnNativeEvent,
        IOnHashlinkVMReady
    {
        public override int Priority => ModulePriorities.Game;

        private void StartGame()
        {
            var entry = (HashlinkClosure)HashlinkMarshal.ConvertHashlinkObject(
                    &HashlinkVM.Instance.Context->c
                    )!;
            entry.CreateDelegate<Action>()();
        }

        void IOnNativeEvent.OnNativeEvent( IOnNativeEvent.Event ev )
        {
            if (ev.EventId == IOnNativeEvent.EventId.HL_EV_START_GAME)
            {
                StartGame();
            }
        }

        private void Hook_Boot_init( HashlinkClosure orig, HashlinkObject self)
        {
            var win = self.AsDynamic().engine.window.window;
            
            orig.DynamicInvoke(self);

            win.set_title("Dead Cells with Core Modding");

            EventSystem.BroadcastEvent<IOnGameInit>();
        }
        private void Hook_Boot_update( HashlinkClosure orig, HashlinkObject self, double dt )
        {
            orig.DynamicInvoke(self, dt);
            EventSystem.BroadcastEvent<IOnFrameUpdate, double>(dt);
        }

        private void Hook_Boot_endInit( HashlinkClosure orig, HashlinkObject self)
        {
            orig.DynamicInvoke(self);

            EventSystem.BroadcastEvent<IOnGameEndInit>();
        }


        private void Hook_Boot_main( HashlinkClosure orig)
        {
            EventSystem.BroadcastEvent<IOnBeforeGameInit>();
            orig.DynamicInvoke();
        }


        void IOnHashlinkVMReady.OnHashlinkVMReady()
        {
            HashlinkHooks.Instance.CreateHook("$Boot", "main", Hook_Boot_main).Enable();
            HashlinkHooks.Instance.CreateHook("Boot", "endInit", Hook_Boot_endInit).Enable();
            HashlinkHooks.Instance.CreateHook("Boot", "init", Hook_Boot_init).Enable();
            HashlinkHooks.Instance.CreateHook("Boot", "update", Hook_Boot_update).Enable();
        }
    }
}
