using System.Runtime.InteropServices;
using dc.achievements;
using Hashlink.Proxy.Clousre;
using ModCore.Storage;
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
            HashlinkHooks.Instance.CreateHook("achievements.SteamAchievementManager", "unlock", Hook_SteamAchievementManager_unlock, true);
            HashlinkHooks.Instance.CreateHook("achievements.SteamAchievementManager", "isUnlocked", Hook_SteamAchievementManager_isUnlocked, true);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                NativeLibrary.SetDllImportResolver(typeof(SteamAPI).Assembly, 
                    ( name, assembly, path ) =>
                {
                    if (name == "steam_api")
                    {
                        return NativeLibrary.Load(
                            FolderInfo.CurrentNativeRoot.GetFilePath("libsteam_api.so")
                        );
                    }
                    return IntPtr.Zero;
                });
            }

            steamAPIInitHook = new(typeof(SteamAPI).GetMethod(nameof(SteamAPI.InitEx))!, Hook_SteamAPI_InitEx);
            steamAPIInitHook.Apply();

            if (SteamAPI.InitEx(out var err) != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Logger.Warning("Unable to initialize the Steam API: {reason}", err);
            }
        }

        private void Hook_SteamAchievementManager_unlock( HashlinkClosure orig, EAchievement arg1 )
        {
            return;
        }

        private bool Hook_SteamAchievementManager_isUnlocked( HashlinkClosure orig, EAchievement arg1 )
        {
            return false;
        }

        private void Hook__Api_sync( HashlinkClosure orig )
        {
            try
            {
                SteamAPI.RunCallbacks();
            }
            catch
            {
                // ignore
            }
        }
    }
}
