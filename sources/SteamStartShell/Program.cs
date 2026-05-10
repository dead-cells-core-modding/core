using ModCore;
using Newtonsoft.Json.Linq;
using Serilog;
using Steamworks;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SteamStartShell
{
    internal class Program
    {
        public const uint MAPI_PFID = 3633185550;
        private static ILogger Logger => Log.Logger;

        public static string gameRoot = ""!;
        public static string crashDumpPath = "";
        public static readonly string ERROR_REPORT_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_error.txt");
        public static readonly string ERROR_REPORT_HEADER = $"""

A Fatal Error / Unhandled Exception has occurred and the game has crashed.

When reporting the issue, please include:

- Crash logs ({ERROR_REPORT_PATH})
- Reproduction steps
- System and version information
- Crash Dump (if available)

You can report or contact us via:

- Discord: https://discord.gg/qH5gw7hwx7

""";

        public static bool enableReporter = true;
        public static string? gameLogLatest;
        private static async Task FindMods()
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
                    var info = (JObject) JToken.Parse(File.ReadAllText(modinfoPath));

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

        private static async Task InitSteamAPI( bool noVerify )
        {
            bool firstAttempt = true;

            _RE_TRY_INIT_STEAM:

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
                goto _RE_TRY_INIT_STEAM;
            }

            if (initResult == ESteamAPIInitResult.k_ESteamAPIInitResult_FailedGeneric)
            {
                await Task.Delay(1000);
                goto _RE_TRY_INIT_STEAM;
            }

            if (initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Logger.Error("Failed to initialize Steam API: {Error} ({StateCode})", err, initResult);
                throw new InvalidOperationException(err);
            }
        }

        private static void CheckNativeLib()
        {
            var steamapiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam_api64.dll");

            if (!File.Exists(steamapiPath))
            {
                Logger.Information("Extracting steam_api64.dll");
                using (var rs = typeof(Program).Assembly.GetManifestResourceStream("steam_api64.dll"))
                {
                    using var fs = File.OpenWrite(steamapiPath);
                    rs!.CopyTo(fs);
                }
            }

            NativeLibrary.Load(steamapiPath);
        }

        private static async Task SteamWork( bool noVerify )
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
                    return;
                }
                throw;
            }

            _RE_TRY:

            await Task.Delay(100);

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
                //Logger.Warning("Not subscribed to DCCM.");
                SteamUGC.SubscribeItem(new(MAPI_PFID));
                await Task.Delay(1000);
                goto _RE_TRY;
            }

            if (!state.HasFlag(EItemState.k_EItemStateInstalled))
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
                    Process.Start(shellPath, ["--update", Environment.ProcessId.ToString(), Environment.ProcessPath!]);
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
                static void CopyDir( DirectoryInfo src, DirectoryInfo dst )
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
                            if (d.Name == ".dotnet")
                            {
                                continue;
                            }
                            CopyDir(d, new(Path.Combine(dst.FullName, d.Name)));
                        }
                    }

                }

                CopyDir(new(mapiFolder), new(Path.Combine(gameRoot, "coremod")));

                File.WriteAllText(Path.Combine(gameRoot, "coremod", "ModCoreVersion.txt"), mccv.ToString());
            }

            Environment.SetEnvironmentVariable("DOTNET_ROOT", Path.Combine(mapiFolder, ".dotnet"));

            await FindMods();

            Environment.SetEnvironmentVariable("DCCM_STEAMWORKSHOP_ENABLED", "true");

            SteamAPI.Shutdown();
        }
        static async Task<int> Main( string[] args )
        {

            try
            {

                if (args.Length > 0 && args[0] == "--update")
                {
                    var pid = int.Parse(args[1]);
                    var dst = args[2];

                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        proc.WaitForExit();
                    }
                    catch (Exception)
                    {
                        //Console.Error.WriteLine(ex);
                    }

                    File.Copy(Environment.ProcessPath!, dst, true);

                    Process.Start(dst);

                    return 0;
                }

                LogInitializer.InitializeLog();


                if (Debugger.IsAttached)
                {
                    Logger.Warning("A debugger has been detected.");

                    Environment.SetEnvironmentVariable("DCCM_SHOULD_WAIT_FOR_DEBUGGER", "true");

                    enableReporter = false;
                }

                if (bool.TryParse(Environment.GetEnvironmentVariable("DCCM_DISABLE_REPORTER"), out var disableReporter) &&
                    disableReporter)
                {
                    enableReporter = false;
                }

                if (bool.TryParse(Environment.GetEnvironmentVariable("DCCM_ENABLE_REPORTER"), out var enableReporterEnv) &&
                   enableReporterEnv)
                {
                    enableReporter = true;
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

                bool noVerify = false;
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skip_verify_steam.txt")))
                {
                    noVerify = true;
                    Logger.Warning("Skipping Steam API verification due to the presence of skip_verify_steam.txt. This may cause issues if Steam is not running or the game is not launched via Steam.");
                }

                CheckNativeLib();

                await SteamWork(noVerify);

                Environment.SetEnvironmentVariable("DCCM_EXIT_WHEN_PROCESS_PID", Environment.ProcessId.ToString());
                Environment.SetEnvironmentVariable("DEAD_CELLS_GAME_PATH", gameRoot);


                bool diagnosticMode = false;

                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diagnostic_mode.txt")))
                {
                    diagnosticMode = true;
                    Environment.SetEnvironmentVariable("DCCM_DIAGNOSTIC_MODE", "true");
                    Logger.Warning("Diagnostic mode enabled.");
                }


                Logger.Information("Starting game...");

                Process? game;

                try
                {
                    game = Process.Start(
                        new ProcessStartInfo(Path.Combine(gameRoot, "coremod", "core", "host", "startup", "DeadCellsModding"))
                        {
                            RedirectStandardError = true,
                            RedirectStandardOutput = diagnosticMode,
                        }
                        );
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 2) //File not found
                {
                    File.Delete(Path.Combine(gameRoot, "coremod", "ModCoreVersion.txt"));
                    Logger.Error("Failed to start the game process. Please restart the game via Steam to fix DCCM.");
                    throw;
                }
                catch (Exception)
                {
                    Logger.Error("Failed to start the game process. Please try deleting {path} to reset DCCM.",
                    Path.GetFullPath(Path.Combine(gameRoot, "coremod")));
                    throw;
                }

                Debug.Assert(game != null);

                StringBuilder errOutputBuilder = new();
                StringBuilder outputBuilder = new();

                game.ErrorDataReceived += ( sender, ev ) =>
                {
                    var data = ev.Data ?? "";

                    if (data.StartsWith("[DCCMLOGLATEST]"))
                    {
                        gameLogLatest = data["[DCCMLOGLATEST]".Length..].Trim();
                        return;
                    }
                    else if (data.StartsWith("[DCCMDBG-CRASH]"))
                    {
                        crashDumpPath = data["[DCCMDBG-CRASH]".Length..].Trim();
                        return;
                    }

                    errOutputBuilder.AppendLine(ev.Data);
                };

                game.BeginErrorReadLine();

                if (diagnosticMode)
                {
                    game.OutputDataReceived += ( sender, ev ) =>
                    {
                        outputBuilder.AppendLine(ev.Data);
                        Console.WriteLine(ev.Data);
                    };
                    game.BeginOutputReadLine();
                }

                await game.WaitForExitAsync();

                if (game.ExitCode == 0)
                {
                    return 0;
                }

                if (!enableReporter)
                {
                    return game.ExitCode;
                }

                Logger.Error(ERROR_REPORT_HEADER);
                Logger.Information("Please check the log file for more detailed information: {path}", ERROR_REPORT_PATH);



                var errText = new StringBuilder();

                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var ntdll = NativeLibrary.Load("ntdll.dll");
                        if (NativeLibrary.TryGetExport(ntdll, "wine_get_version", out var wineVerFunc))
                        {
                            unsafe
                            {
                                var ver = Marshal.PtrToStringUTF8((nint)((delegate* unmanaged< byte* >)wineVerFunc)()) ?? "Unknown";
                                errText.AppendLine();
                                errText.Append("The current program is running on Proton/Wine: ");
                                errText.AppendLine(ver);
                                errText.AppendLine();
                            }
                        }
                    }
                }



                if (errOutputBuilder.Length > 0)
                {
                    errText.AppendLine("\n:============ BELOW IS THE ERRORS ============:\n");
                    errText.AppendLine(errOutputBuilder.ToString());
                    errText.AppendLine();
                }

                if (!string.IsNullOrEmpty(gameLogLatest))
                {
                    errText.AppendLine("\n:============ BELOW IS THE GAME LOG ============:\n");
                    errText.AppendLine(File.ReadAllText(gameLogLatest));
                    errText.AppendLine();
                }

                if (outputBuilder.Length > 0)
                {
                    errText.AppendLine("\n:============ BELOW IS FULL OUTPUT ============:\n");
                    errText.AppendLine(outputBuilder.ToString());
                    errText.AppendLine();
                }

                var err = new StringBuilder();

                err.AppendLine(ERROR_REPORT_HEADER);

                if (!string.IsNullOrEmpty(crashDumpPath) &&
                    File.Exists(crashDumpPath))
                {
                    err.AppendLine();
                    err.AppendLine("You may also need to send this crash dump: " + crashDumpPath);
                    err.AppendLine();
                }

                var errTextStr = errText.ToString();

                err.AppendLine(errTextStr);


                File.WriteAllText(ERROR_REPORT_PATH, err.ToString());

                Process.Start(new ProcessStartInfo(ERROR_REPORT_PATH)
                {
                    UseShellExecute = true
                });

                return game.ExitCode;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Fatal error occurred: {Message}", ex.Message);
                await Task.Delay(5000);
                throw;
            }
        }
    }
}
