
using dc;
using dc.haxe;
using dc.hxd;
using dc.hxd.res;
using dc.pr;
using dc.ui;
using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using HaxeProxy.Runtime;
using ModCore.Events.Interfaces;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal | CoreModuleAttribute.CoreModuleKind.Important)]
    internal unsafe class GameStartup : CoreModule<GameStartup>, IOnNativeEvent
    {
        private void BakeCode()
        {
            List<System.Type> types = [
                typeof(_Boot),
                typeof(Boot),
                typeof(App),
                typeof(_App),
                typeof(Main),
                typeof(_Main),
                typeof(TitleScreen),
                typeof(_TitleScreen),
                typeof(dc.hxd.fmt.pak.FileSystem),
                typeof(dc.hxd.fmt.pak._FileSystem),
                typeof(_Sys),
                typeof(dc.libs.Process),
                typeof(dc.libs._Process),
                //typeof(Assets),
                //typeof(_Assets),
                typeof(AssetsLibManager),
                typeof(_AssetsLibManager),
                typeof(MainLoop),
                typeof(_MainLoop),
                typeof(_System),
                typeof(dc.hxd.System),
                typeof(_EntryPoint),
                typeof(BitmapFont),
                typeof(_BitmapFont)
                ];



            //foreach (var t in typeof(_Boot).Assembly.GetTypes())
            //{
            //    if (t.IsAssignableTo(typeof(HaxeObject)))
            //    {
            //        types.Add(t);
            //    }
            //}

            Logger.Information("Cook types: {num}", types.Count);

            foreach (var t in types)
            {
                HaxeProxyUtils.UseCSharpVersionCode(t, false);
            }

            Logger.Information("Cook types finished!");
        }

        private void StartGame()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            //BakeCode();

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
                    System.Console.Error.WriteLine("[DCCM_FORCE_SHOW_ERROR_REPORT]");
                    Environment.Exit(-1);
                    throw;
                }
            }
        }
    }
}
