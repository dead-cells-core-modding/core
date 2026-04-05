using dc.steam;
using MonoMod.RuntimeDetour;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Modules.Platforms
{
    internal class SteamPlatformModule : Module<SteamPlatformModule>
    {
        private readonly Hook steamAPIInitHook;
        private delegate ESteamAPIInitResult Orig_SteamAPI_InitEx( out string? err );
        private ESteamAPIInitResult Hook_SteamAPI_InitEx( Orig_SteamAPI_InitEx orig, out string? err)
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
            Hook__Api.sync += Hook__Api_sync;

            steamAPIInitHook = new(typeof(SteamAPI).GetMethod(nameof(SteamAPI.InitEx))!, Hook_SteamAPI_InitEx);
            steamAPIInitHook.Apply();

            if (SteamAPI.InitEx(out var err) != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Logger.Warning("Unable to initialize the Steam API: {reason}", err);
            }
        }

        private void Hook__Api_sync( Hook__Api.orig_sync orig )
        {
            SteamAPI.RunCallbacks();
        }
    }
}
