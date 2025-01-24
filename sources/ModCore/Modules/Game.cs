using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using ModCore.Events.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class Game : CoreModule<Game>, IOnNativeEvent
    {
        public override int Priority => ModulePriorities.Game;

        private void StartGame()
        {
            try
            {
                var entry = (HashlinkClosure)HashlinkMarshal.ConvertHashlinkObject(
                        &HashlinkVM.Instance.Context->c
                        );
                entry.Function.Call();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Uncaught exception.");
                Environment.Exit(-1);
            }
        }

        void IOnNativeEvent.OnNativeEvent(IOnNativeEvent.Event ev)
        {
            if (ev.EventId == IOnNativeEvent.EventId.HL_EV_START_GAME)
            {
                StartGame();
            }
        }
    }
}
