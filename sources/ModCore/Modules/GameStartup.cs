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
using System.Diagnostics;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    internal unsafe class GameStartup : CoreModule<GameStartup>, IOnNativeEvent
    {
        

        private void StartGame()
        {
            var entry = (HashlinkClosure)HashlinkMarshal.ConvertHashlinkObject(
                    &HashlinkVM.Instance.Context->c
                    )!;
            hl_blocking(1);
            var action = entry.CreateDelegate<Action>();
            action();
            hl_blocking(0);
        }

        void IOnNativeEvent.OnNativeEvent( IOnNativeEvent.Event ev )
        {
            if (ev.EventId == IOnNativeEvent.EventId.HL_EV_START_GAME)
            {
                try
                {
                    StartGame();
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex, "Fatal Error");
                    Environment.Exit(-1);
                    throw;
                }
            }
        }
    }
}
