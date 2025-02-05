using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using Haxe;
using Haxe.Marshaling;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Events.Interfaces.VM;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class Game : CoreModule<Game>, IOnNativeEvent,
        IOnHashlinkVMReady
    {
        public override int Priority => ModulePriorities.Game;

        private void StartGame()
        {
            try
            {
                var entry = (HashlinkClosure)HashlinkMarshal.ConvertHashlinkObject(
                        &HashlinkVM.Instance.Context->c
                        )!;
                entry.Function.Call();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Uncaught exception.");
                Environment.Exit(-1);
            }
        }

        void IOnNativeEvent.OnNativeEvent( IOnNativeEvent.Event ev )
        {
            if (ev.EventId == IOnNativeEvent.EventId.HL_EV_START_GAME)
            {
                EventSystem.BroadcastEvent<IOnBeforeGameStart>();
                StartGame();
            }
        }

        private object? Hook_Boot_init(HashlinkFunc orig, HashlinkObject self)
        {
            var win = self.AsHaxe().Chain.engine.window.window;
            
            var ret = orig.Call(self);

            win.set_title("Dead Cells with Core Modding");

            EventSystem.BroadcastEvent<IOnGameInit>();

            return ret;
        }
        private object? Hook_Boot_update( HashlinkFunc orig, HashlinkObject self, double dt )
        {
            var ret = orig.Call(self, dt);

            EventSystem.BroadcastEvent<IOnFrameUpdate, double>(dt);

            if (HeroInstance != null)
            {
                EventSystem.BroadcastEvent<IOnHeroUpdate, double>(dt);
            }

            return ret;
        }
        private object? Hook_Boot_endInit( HashlinkFunc orig, HashlinkObject self)
        {
            var ret = orig.Call(self);

            EventSystem.BroadcastEvent<IOnGameEndInit>();

            return ret;
        }

        public HaxeObject? HeroInstance
        {
            get; private set;
        }
        private object? Hook_hero_init( HashlinkFunc orig, HashlinkObject self )
        {
            HeroInstance = self.AsHaxe();
            EventSystem.BroadcastEvent<IOnHeroInit>();
            return orig.Call(self);
        }
        private object? Hook_hero_dispose( HashlinkFunc orig, HashlinkObject self )
        {
            EventSystem.BroadcastEvent<IOnHeroUpdate>();
            HeroInstance = null;
            return orig.Call(self);
        }


        void IOnHashlinkVMReady.OnHashlinkVMReady()
        {

            HashlinkHooks.Instance.CreateHook("Boot", "endInit", Hook_Boot_endInit).Enable();
            HashlinkHooks.Instance.CreateHook("Boot", "init", Hook_Boot_init).Enable();
            HashlinkHooks.Instance.CreateHook("Boot", "update", Hook_Boot_update).Enable();

            HashlinkHooks.Instance.CreateHook("en.Hero", "init", Hook_hero_init).Enable();
            HashlinkHooks.Instance.CreateHook("en.Hero", "dispose", Hook_hero_dispose).Enable();
        }
    }
}
