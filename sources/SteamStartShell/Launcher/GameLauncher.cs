using Serilog;
using SteamStartShell.Platform;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace SteamStartShell.Launcher
{
    /// <summary>
    /// 游戏启动器——设置环境变量并启动 DeadCellsModding 子进程
    /// </summary>
    internal class GameLauncher
    {
        private readonly string _gameRoot;
        private static ILogger Logger => Log.Logger;

        public GameLauncher(string gameRoot)
        {
            _gameRoot = gameRoot;
        }

        /// <summary>
        /// 游戏启动结果
        /// </summary>
        public class LaunchResult
        {
            public int ExitCode { get; init; }
            public string? ErrorOutput { get; init; }
            public string? OutputData { get; init; }
            public string? GameLogPath { get; init; }
            public string? CrashDumpPath { get; init; }
        }

        /// <summary>
        /// 启动 DeadCellsModding 进程
        /// </summary>
        /// <param name="deadCellsExePath">DeadCellsModding 可执行文件路径</param>
        /// <param name="parentPid">父进程 PID，用于子进程的退出监听</param>
        /// <param name="diagnosticMode">是否启用诊断模式（捕获 stdout）</param>
        /// <returns>启动结果</returns>
        public async Task<LaunchResult> LaunchGame(
            string deadCellsExePath,
            int parentPid,
            bool diagnosticMode)
        {
            Environment.SetEnvironmentVariable("DCCM_EXIT_WHEN_PROCESS_PID", parentPid.ToString());
            Environment.SetEnvironmentVariable("DEAD_CELLS_GAME_PATH", _gameRoot);

            if (diagnosticMode)
            {
                Environment.SetEnvironmentVariable("DCCM_DIAGNOSTIC_MODE", "true");
                Logger.Warning("Diagnostic mode enabled.");
            }

            Logger.Information("Starting game...");

            Process? game;
            var psi = PlatformServices.Current.ConfigureGameProcess(deadCellsExePath)
                      ?? new ProcessStartInfo(deadCellsExePath)
                      {
                          RedirectStandardError = true,
                          RedirectStandardOutput = diagnosticMode,
                      };

            // 覆盖平台默认值——确保错误输出始终被重定向
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = diagnosticMode;

            try
            {
                game = Process.Start(psi);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2) // File not found
            {
                File.Delete(Path.Combine(_gameRoot, "coremod", "ModCoreVersion.txt"));
                Logger.Error("Failed to start the game process. Please restart the game via Steam to fix DCCM.");
                throw;
            }
            catch (Exception)
            {
                Logger.Error("Failed to start the game process. Please try deleting {path} to reset DCCM.",
                    Path.GetFullPath(Path.Combine(_gameRoot, "coremod")));
                throw;
            }

            Debug.Assert(game != null);

            StringBuilder errOutputBuilder = new();
            StringBuilder outputBuilder = new();
            string? gameLogLatest = null;
            string? crashDumpPath = null;

            game.ErrorDataReceived += (sender, ev) =>
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
                game.OutputDataReceived += (sender, ev) =>
                {
                    outputBuilder.AppendLine(ev.Data);
                    Console.WriteLine(ev.Data);
                };
                game.BeginOutputReadLine();
            }

            await game.WaitForExitAsync();

            return new LaunchResult
            {
                ExitCode = game.ExitCode,
                ErrorOutput = errOutputBuilder.Length > 0 ? errOutputBuilder.ToString() : null,
                OutputData = outputBuilder.Length > 0 ? outputBuilder.ToString() : null,
                GameLogPath = gameLogLatest,
                CrashDumpPath = crashDumpPath,
            };
        }
    }
}
