using Newtonsoft.Json.Linq;
using Serilog;
using SteamLauncher.Platform;
using Steamworks;
using System.Diagnostics;

namespace SteamLauncher.Workshop
{
    /// <summary>
    /// Steam Workshop manager — handles Steam API initialization, DCCM Workshop download/update, Shell self-update, and ModCore version check
    /// </summary>
    internal class SteamWorkshopManager
    {
        public const uint MAPI_PFID = 3633185550;

        private readonly string _gameRoot;
        private static ILogger Logger => Log.Logger;

        /// <summary>
        /// Represents the result of a SteamWork operation
        /// </summary>
        public enum SteamWorkResult
        {
            /// <summary>Completed successfully</summary>
            Success,
            /// <summary>Shell self-update required</summary>
            ShellUpdateRequired,
            /// <summary>Steam API verification skipped</summary>
            NoVerifySkipped
        }

        public SteamWorkshopManager(string gameRoot)
        {
            _gameRoot = gameRoot;
        }

        /// <summary>
        /// Categorizes Steam UGC subscriptions into mods and plugins, and sets corresponding environment variables
        /// </summary>
        public async Task FindMods()
        {
            Logger.Information("Finding mods...");

            var count = SteamUGC.GetNumSubscribedItems(false);

            var items = new PublishedFileId_t[count];

            SteamUGC.GetSubscribedItems(items, count, false);

            List<string> mods = [];
            List<string> plugins = [];

            foreach (var v in items)
            {
                if (SteamUGC.GetItemInstallInfo(v, out var _, out var modPath, 1024, out _))
                {
                    var modinfoPath = Path.Combine(modPath, "modinfo.json");
                    if (!File.Exists(modinfoPath))
                    {
                        continue;
                    }
                    var info = (JObject)JToken.Parse(File.ReadAllText(modinfoPath));

                    Logger.Information("Found workshop mod: {name} {path}", info["name"]?.ToString(), modinfoPath);

                    if (info["type"]?.ToString() == "plugin")
                    {
                        plugins.Add(modPath);
                    }
                    else
                    {
                        mods.Add(modPath);
                    }
                }
            }
            Environment.SetEnvironmentVariable("DCCM_EXTRA_PLUGINS_PATHS", string.Join(';', plugins));
            Environment.SetEnvironmentVariable("DCCM_EXTRA_MODS_PATHS", string.Join(';', mods));
        }

        /// <summary>
        /// Initializes the Steam API — refactored goto retry loop into a while loop
        /// </summary>
        private async Task InitSteamAPI(bool noVerify)
        {
            bool firstAttempt = true;

            while (true)
            {
                var initResult = SteamAPI.InitEx(out var err);

                if (noVerify &&
                    initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    throw new InvalidOperationException("Failed to init Steam API.");
                }

                if (initResult == ESteamAPIInitResult.k_ESteamAPIInitResult_NoSteamClient)
                {
                    if (firstAttempt)
                    {
                        firstAttempt = false;
                        Logger.Information("Launching steam...");
                        Process.Start(new ProcessStartInfo("steam://run")
                        {
                            UseShellExecute = true,
                        });
                    }
                    await Task.Delay(500);
                    continue;
                }

                if (initResult == ESteamAPIInitResult.k_ESteamAPIInitResult_FailedGeneric)
                {
                    await Task.Delay(1000);
                    continue;
                }

                if (initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    Logger.Error("Failed to initialize Steam API: {Error} ({StateCode})", err, initResult);
                    throw new InvalidOperationException(err);
                }

                // Initialization succeeded, exit loop
                break;
            }
        }

        /// <summary>
        /// Executes the Steam Workshop workflow: download/update DCCM, Shell self-update check, ModCore version check
        /// </summary>
        /// <returns>Operation result status</returns>
        public async Task<SteamWorkResult> SteamWork(bool noVerify)
        {
            Environment.SetEnvironmentVariable("SteamAPPId", "588650");

            Logger.Information("Trying to load steam api");

            try
            {
                await InitSteamAPI(noVerify).WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (Exception)
            {
                Logger.Error("Unable to initialize SteamAPI. Please ensure Steam is running and you own the game.");
                if (noVerify)
                {
                    return SteamWorkResult.NoVerifySkipped;
                }
                throw;
            }

            SteamUGC.SubscribeItem(new(MAPI_PFID));
            SteamUGC.DownloadItem(new(MAPI_PFID), true);

            await Task.Delay(100);

            while (true)
            {
                await Task.Delay(100);

                var state = (EItemState)SteamUGC.GetItemState(new(MAPI_PFID));

                if (state.HasFlag(EItemState.k_EItemStateDownloading) ||
                    state.HasFlag(EItemState.k_EItemStateDownloadPending))
                {
                    Logger.Information("DCCM is downloading...");
                    while (state.HasFlag(EItemState.k_EItemStateDownloading) ||
                           state.HasFlag(EItemState.k_EItemStateDownloadPending))
                    {
                        await Task.Delay(3000);
                        state = (EItemState)SteamUGC.GetItemState(new(MAPI_PFID));
                    }
                    continue;
                }

                if (!state.HasFlag(EItemState.k_EItemStateSubscribed))
                {
                    SteamUGC.SubscribeItem(new(MAPI_PFID));
                    await Task.Delay(1000);
                    continue;
                }

                if (!state.HasFlag(EItemState.k_EItemStateInstalled))
                {
                    Logger.Warning("DCCM is not installed.");
                    SteamUGC.DownloadItem(new(MAPI_PFID), true);
                    continue;
                }

                if (state.HasFlag(EItemState.k_EItemStateNeedsUpdate))
                {
                    Logger.Information("DCCM needs update.");
                    SteamUGC.DownloadItem(new(MAPI_PFID), true);
                    continue;
                }

                if (!SteamUGC.GetItemInstallInfo(new(MAPI_PFID), out _, out var mapiFolder, 1024, out _))
                {
                    Logger.Warning("DCCM is not installed.");
                    continue;
                }

                mapiFolder = Path.Combine(mapiFolder, PlatformServices.Current.Name);

                Logger.Information("DCCM Workshop Version Path: {path}", mapiFolder);

                // Check Shell self-update
                var shellPath = Path.Combine(mapiFolder, "content", "core", "host", "startup", "steam", "deadcells.exe");
                if (File.Exists(shellPath))
                {
                    var ws_shellVer = System.Version.Parse(
                        FileVersionInfo.GetVersionInfo(shellPath).FileVersion!);
                    var cur_shellVer = System.Version.Parse(
                        FileVersionInfo.GetVersionInfo(Environment.ProcessPath!).FileVersion!);
                    Logger.Information("Current Shell Version: {ver}", cur_shellVer);
                    Logger.Information("Workshop Shell Version: {ver}", ws_shellVer);

                    if (ws_shellVer > cur_shellVer)
                    {
                        Logger.Information("Updating Shell to Workshop Version...");
                        Process.Start(shellPath, ["--update",
                            Environment.ProcessId.ToString(),
                            Environment.ProcessPath!]);
                        return SteamWorkResult.ShellUpdateRequired;
                    }
                }

                var mccv_path = Path.Combine(mapiFolder, "content", "ModCoreVersion.txt");

                if (!File.Exists(mccv_path))
                {
                    Logger.Warning("DCCM is not installed.");
                    SteamUGC.DownloadItem(new(MAPI_PFID), true);
                    continue;
                }

                var mccv = System.Version.Parse(File.ReadAllText(mccv_path).Trim());
                var cur_mccv_path = Path.Combine(_gameRoot, "coremod", "ModCoreVersion.txt");

                var needUpdateModCore = false;

                Logger.Information("Workshop DCCM Version: {ver}", mccv);

                if (File.Exists(cur_mccv_path))
                {
                    var cur_mccv = System.Version.Parse(File.ReadAllText(cur_mccv_path).Trim());

                    Logger.Information("Current DCCM Version: {ver}", cur_mccv);

                    if (mccv > cur_mccv)
                    {
                        Logger.Information("Updating DCCM...");
                        needUpdateModCore = true;
                    }
                }
                else
                {
                    Logger.Information("Installing DCCM...");
                    needUpdateModCore = true;
                }

                if (needUpdateModCore)
                {
                    static void CopyDir(DirectoryInfo src, DirectoryInfo dst)
                    {
                        dst.Create();

                        foreach (var fi in src.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
                        {
                            if (fi is FileInfo f)
                            {
                                if (f.Name == "ModCoreVersion.txt" ||
                                    f.Name == "deadcells.exe" ||
                                    f.Name == "deadcells" ||
                                    f.Name == "deadcells.pdb")
                                {
                                    continue;
                                }
                                var tf = Path.GetFullPath(Path.Combine(dst.FullName, f.Name));
                                f.CopyTo(tf, true);
                            }
                            else if (fi is DirectoryInfo d)
                            {
                                CopyDir(d, new(Path.Combine(dst.FullName, d.Name)));
                            }
                        }
                    }

                    CopyDir(new(Path.Combine(mapiFolder, "content")), new(Path.Combine(_gameRoot, "coremod")));

                    File.WriteAllText(Path.Combine(_gameRoot, "coremod", "ModCoreVersion.txt"), mccv.ToString());
                }

                Environment.SetEnvironmentVariable("DOTNET_ROOT", Path.Combine(mapiFolder, ".dotnet"));

                await FindMods();

                Environment.SetEnvironmentVariable("DCCM_STEAMWORKSHOP_ENABLED", "true");

                SteamAPI.Shutdown();

                return SteamWorkResult.Success;
            }
        }
    }
}
