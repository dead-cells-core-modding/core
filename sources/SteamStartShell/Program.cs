using ModCore;
using Serilog;
using Steamworks;
using System.Diagnostics;

namespace SteamStartShell
{
    internal class Program
    {
        public const uint MAPI_PFID = 3633185550;
        private static ILogger Logger => Log.Logger;

        public static string gameRoot = ""!;

        private static async Task SteamWork()
        {

            Environment.SetEnvironmentVariable("SteamAPPId", "588650");

            Logger.Information("Trying to load steam api");

            var initResult = SteamAPI.InitEx(out var err);

            if (initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Logger.Error("Failed to initialize Steam API: {Error} ({StateCode})", err, initResult);
                return;
            }



            _RE_TRY:

            var state = (EItemState)SteamUGC.GetItemState(new(MAPI_PFID));

            if (state.HasFlag(EItemState.k_EItemStateDownloading) || state.HasFlag(EItemState.k_EItemStateDownloadPending))
            {
                Logger.Information("DCCM is downloading...");
                while (state.HasFlag(EItemState.k_EItemStateDownloading) || state.HasFlag(EItemState.k_EItemStateDownloadPending))
                {
                    await Task.Delay(3000);
                    state = (EItemState)SteamUGC.GetItemState(new(MAPI_PFID));
                }
                goto _RE_TRY;
            }

            if (!state.HasFlag(EItemState.k_EItemStateSubscribed))
            {
                Logger.Warning("Not subscribed to DCCM.");
                SteamUGC.SubscribeItem(new(MAPI_PFID));
                goto _RE_TRY;
            }

            if(!state.HasFlag(EItemState.k_EItemStateInstalled))
            {
                Logger.Warning("DCCM is not installed.");
                SteamUGC.DownloadItem(new(MAPI_PFID), true);
                goto _RE_TRY;
            }

            if (state.HasFlag(EItemState.k_EItemStateNeedsUpdate))
            {
                Logger.Information("DCCM needs update.");
                SteamUGC.DownloadItem(new(MAPI_PFID), true);
                goto _RE_TRY;
            }

            if (!SteamUGC.GetItemInstallInfo(new(MAPI_PFID), out _, out var mapiFolder, 1024, out var lastUpdateTime))
            {
                Logger.Warning("DCCM is not installed.");

                goto _RE_TRY;
            }

            Logger.Information("DCCM Workshop Version Path: {path}", mapiFolder);

            // Check for shell update
            var shellPath = Path.Combine(mapiFolder, "core", "host", "startup", "steam", "deadcells.exe");
            if (File.Exists(shellPath))
            {
                var ws_shellVer = System.Version.Parse(FileVersionInfo.GetVersionInfo(shellPath).FileVersion!);
                var cur_shellVer = System.Version.Parse(FileVersionInfo.GetVersionInfo(Environment.ProcessPath!).FileVersion!);
                Logger.Information("Current Shell Version: {ver}", cur_shellVer);
                Logger.Information("Workshop Shell Version: {ver}", ws_shellVer);

                if (ws_shellVer > cur_shellVer)
                {
                    Logger.Information("Updating Shell to Workshop Version...");
                    Process.Start(shellPath, [  "--update", Environment.ProcessId.ToString(), Environment.ProcessPath! ]);
                    Environment.Exit(0);
                }
            }

            var mccv_path = Path.Combine(mapiFolder, "ModCoreVersion.txt");

            if (!File.Exists(mccv_path))
            {
                Logger.Warning("DCCM is not installed.");
                SteamUGC.DownloadItem(new(MAPI_PFID), true);
                goto _RE_TRY;
            }

            var mccv = System.Version.Parse(File.ReadAllText(mccv_path).Trim());
            var cur_mccv_path = Path.Combine(gameRoot, "coremod", "ModCoreVersion.txt");

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
                            f.CopyTo(Path.Combine(dst.FullName, f.Name), true);
                        }
                        else if (fi is DirectoryInfo d)
                        {
                            CopyDir(d, new(Path.Combine(dst.FullName, d.Name)));
                        }
                    }

                }

                CopyDir(new(mapiFolder), new(Path.Combine(gameRoot, "coremod")));
            }

            SteamAPI.Shutdown();
        }
        static async Task<int> Main( string[] args )
        {
            if(args.Length > 0 && args[0] == "--update")
            {
                var pid = int.Parse(args[1]);
                var dst = args[2];

                try
                {
                    var proc = Process.GetProcessById(pid);
                    proc.WaitForExit();
                }
                catch(Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }

                File.Copy(Environment.ProcessPath!, dst, true);

                Process.Start(dst);

                return 0;
            }

            gameRoot = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH")!;
            if (string.IsNullOrEmpty(gameRoot))
            {
                gameRoot = Path.GetDirectoryName(Environment.ProcessPath!)!;
            }

            gameRoot = Path.GetFullPath(gameRoot);
            while (!string.IsNullOrEmpty(gameRoot))
            {
                var modcore = Path.GetFullPath(Path.Combine(gameRoot, "deadcells_gl.exe"));
#if DEBUG
                Console.WriteLine("Try find deadcells_gl in " + modcore);
#endif
                if (File.Exists(modcore))
                {
                    if (Environment.ProcessPath != modcore)
                    {
                        break;
                    }
                }
                gameRoot = Path.GetDirectoryName(gameRoot)!;
            }

            if (string.IsNullOrEmpty(gameRoot))
            {
                Logger.Error("Game directory not found.");
                Environment.Exit(-1);
            }

            Directory.SetCurrentDirectory(gameRoot!);

            LogInitializer.InitializeLog();

            await SteamWork();

            Environment.SetEnvironmentVariable("DEAD_CELLS_GAME_PATH", gameRoot);
            Environment.SetEnvironmentVariable("DOTNET_ROOT", Path.Combine(gameRoot, "coremod", ".dotnet"));

            var game = Process.Start(Path.Combine(gameRoot, "coremod", "core", "host", "startup", "DeadCellsModding.exe"));

            await game.WaitForExitAsync();

            return game.ExitCode;
        }
    }
}
