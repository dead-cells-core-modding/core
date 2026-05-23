using ModCore;
using Serilog;
using SteamStartShell.ErrorReporting;
using SteamStartShell.Launcher;
using SteamStartShell.Platform;
using SteamStartShell.Workshop;
using System.Diagnostics;

namespace SteamStartShell
{
    internal class Program
    {
        private static ILogger Logger => Log.Logger;

        static async Task<int> Main(string[] args)
        {
            try
            {
                // 处理 --update 命令：等待旧进程退出后替换 shell 可执行文件
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
                        // 忽略进程已退出等错误
                    }

                    File.Copy(Environment.ProcessPath!, dst, true);

                    Process.Start(dst);

                    return 0;
                }

                LogInitializer.InitializeLog();

                // 确定是否启用错误报告器
                bool enableReporter = true;

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

                // 定位游戏根目录
                string gameRoot = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH")!;
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

                // 是否跳过 Steam API 验证
                bool noVerify = false;
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skip_verify_steam.txt")))
                {
                    noVerify = true;
                    Logger.Warning("Skipping Steam API verification due to the presence of skip_verify_steam.txt. " +
                                   "This may cause issues if Steam is not running or the game is not launched via Steam.");
                }

                // 加载平台原生库
                PlatformServices.Current.CheckNativeLib();

                // 执行 Steam Workshop 工作流程
                var workshopManager = new SteamWorkshopManager(gameRoot);
                var steamResult = await workshopManager.SteamWork(noVerify);

                if (steamResult == SteamWorkshopManager.SteamWorkResult.ShellUpdateRequired)
                {
                    // Shell 自更新信号——已启动新进程，退出当前进程
                    Environment.Exit(0);
                }

                if (steamResult == SteamWorkshopManager.SteamWorkResult.NoVerifySkipped)
                {
                    // Steam API 不可用但跳过了验证——继续执行但禁用了 Workshop
                }

                // 检测诊断模式
                bool diagnosticMode = false;
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diagnostic_mode.txt")))
                {
                    diagnosticMode = true;
                }

                // 启动游戏
                var launcher = new GameLauncher(gameRoot);
                var deadCellsExePath = Path.Combine(gameRoot, "coremod", "core", "host", "startup", "DeadCellsModding");
                var result = await launcher.LaunchGame(deadCellsExePath, Environment.ProcessId, diagnosticMode);

                if (result.ExitCode == 0)
                {
                    return 0;
                }

                if (!enableReporter)
                {
                    return result.ExitCode;
                }

                // 生成错误报告
                var reporter = new ErrorReportGenerator();
                reporter.GenerateReport(
                    result.ExitCode,
                    result.ErrorOutput,
                    result.OutputData,
                    result.GameLogPath,
                    result.CrashDumpPath);

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
