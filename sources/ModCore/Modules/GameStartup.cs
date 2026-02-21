using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using ModCore.Events.Interfaces;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    internal unsafe class GameStartup : CoreModule<GameStartup>, IOnNativeEvent
    {
        private void StartGame()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var entry = (HashlinkClosure)HashlinkMarshal.ConvertHashlinkObject(
                    &HashlinkVM.Instance.Context->c
                    )!;
            hl_blocking(1);
            var action = entry.CreateDelegate<Action>();
            action();
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
