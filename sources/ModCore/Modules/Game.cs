using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using Haxe.Marshaling;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
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

            return ret;
        }

        void IOnHashlinkVMReady.OnHashlinkVMReady()
        {
            unchecked
            {

                var m = HashlinkMarshal.Module;
                var boot = (HashlinkObjectType)m.GetTypeByName("Boot");
                var a = boot.FindProto("init");
                var hook = HashlinkHooks.Instance.CreateHook(a!.Function, Hook_Boot_init);
                hook.Enable();
            }
        }
    }
}
