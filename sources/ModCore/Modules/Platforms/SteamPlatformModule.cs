using Hashlink.Proxy.Clousre;
using MonoMod.RuntimeDetour;
using Steamworks;

namespace ModCore.Modules.Platforms
{
    internal class SteamPlatformModule : Module<SteamPlatformModule>
    {
        private readonly Hook steamAPIInitHook;
        private delegate ESteamAPIInitResult Orig_SteamAPI_InitEx( out string? err );
        private ESteamAPIInitResult Hook_SteamAPI_InitEx( Orig_SteamAPI_InitEx orig, out string? err )
        {
            try
            {
                InteropHelp.TestIfAvailableClient();

                err = null;
                return ESteamAPIInitResult.k_ESteamAPIInitResult_OK;
            }
            catch
            {

            }
            return orig(out err);
        }

        public SteamPlatformModule()
        {
            HashlinkHooks.Instance.CreateHook("steam.$Api", "sync", Hook__Api_sync, true);

            steamAPIInitHook = new(typeof(SteamAPI).GetMethod(nameof(SteamAPI.InitEx))!, Hook_SteamAPI_InitEx);
            steamAPIInitHook.Apply();

            if (SteamAPI.InitEx(out var err) != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Logger.Warning("Unable to initialize the Steam API: {reason}", err);
            }
        }

        private void Hook__Api_sync( HashlinkClosure orig )
        {
            SteamAPI.RunCallbacks();
        }
    }
}
