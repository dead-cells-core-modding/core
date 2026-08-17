using Serilog;
using SteamLauncher.Platform;
using SteamLauncher.Workshop;
using SteamLauncher.Launcher;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SteamLauncher
{
    public static class Program
    {
        private static ILogger Logger => Log.Logger;

        public static async Task<int> Main(string[] args)
        {
            Environment.SetEnvironmentVariable("DCCM_START_BY_LAUNCHER", "true");
            try
            {
                if(args.Length > 0 && args[0].EndsWith(".hl", StringComparison.Ordinal))
                {
                    return 0;
                }
                // Handle --update command: wait for old process to exit then replace shell executable
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
                        // Ignore errors such as process already exited
                    }

                    File.Copy(Environment.ProcessPath!, dst, true);

                    Process.Start(dst);

                    return 0;
                }

                LogInitializer.InitializeLog();

                // Locate game root directory
                string gameRoot = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH")!;
                if (string.IsNullOrEmpty(gameRoot))
                {
                    gameRoot = Path.GetDirectoryName(Environment.ProcessPath!)!;
                }

                gameRoot = Path.GetFullPath(gameRoot);
                while (!string.IsNullOrEmpty(gameRoot))
                {
                    var modcore = Path.GetFullPath(Path.Combine(gameRoot, 
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "deadcells_gl.exe" : "hlboot.dat"
                        ));
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

                if (!bool.TryParse(Environment.GetEnvironmentVariable("DCCM_LAUNCHER_NOT_STEAM"), out var noSteam) && !noSteam)
                {
                    Logger.Information("Steam version.");

                    // Whether to skip Steam API verification
                    bool noVerify = false;
                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skip_verify_steam.txt")))
                    {
                        noVerify = true;
                        Logger.Warning("Skipping Steam API verification due to the presence of skip_verify_steam.txt. " +
                                       "This may cause issues if Steam is not running or the game is not launched via Steam.");
                    }

                    // Load platform native library
                    PlatformServices.Current.CheckNativeLib();

                    // Execute Steam Workshop workflow
                    var workshopManager = new SteamWorkshopManager(gameRoot);
                    var steamResult = await workshopManager.SteamWork(noVerify);

                    if (steamResult == SteamWorkshopManager.SteamWorkResult.ShellUpdateRequired)
                    {
                        // Shell self-update signal — new process started, exit current process
                        Environment.Exit(0);
                    }

                    if (steamResult == SteamWorkshopManager.SteamWorkResult.NoVerifySkipped)
                    {
                        // Steam API unavailable but verification skipped — continue with Workshop disabled
                        Logger.Warning("Steam is skiped");
                    }
                }
                else
                {
                    Logger.Information("Not Steam version.");
                }

                // Launch game
                var launcher = new GameLauncher(gameRoot);
                var deadCellsExePath = Path.Combine(gameRoot, "coremod", "core", "host", "startup", "DeadCellsModding");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    File.SetUnixFileMode(deadCellsExePath, File.GetUnixFileMode(deadCellsExePath) | UnixFileMode.UserExecute);
                }

                if (bool.TryParse(Environment.GetEnvironmentVariable("DCCM_SHELL_CHECK_ONLY"), out var checkOnly) && checkOnly)
                {
                    return 0;
                }

                var result = await launcher.LaunchGame(deadCellsExePath, Environment.ProcessId);

                if (result.ExitCode != 0)
                {
                    await Task.Delay(5000);
                }

                return result.ExitCode;
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
