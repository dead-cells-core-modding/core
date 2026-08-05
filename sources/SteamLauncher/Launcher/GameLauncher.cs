using Serilog;
using SteamLauncher.Platform;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace SteamLauncher.Launcher
{
    /// <summary>
    /// Game launcher — sets environment variables and launches the DeadCellsModding child process
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
        /// Game launch result
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
        /// Launches the DeadCellsModding process
        /// </summary>
        /// <param name="deadCellsExePath">Path to the DeadCellsModding executable</param>
        /// <param name="parentPid">Parent process PID, used for child process exit monitoring</param>
        /// <param name="diagnosticMode">Whether to enable diagnostic mode (captures stdout)</param>
        /// <returns>Launch result</returns>
        public async Task<LaunchResult> LaunchGame(
            string deadCellsExePath,
            int parentPid,
            bool diagnosticMode)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT")))
            {
                Logger.Warning("DOTNET_ROOT is empty!");
            }

            Logger.Information("DOTNET_ROOT: {root}", Environment.GetEnvironmentVariable("DOTNET_ROOT"));

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

            // Override platform defaults — ensure error output is always redirected
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
                Logger.Error("Failed to start the game process. Please try deleting '{path}' to reset DCCM.",
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

                if (data.StartsWith("[DCCMLOGLATEST]", StringComparison.Ordinal))
                {
                    gameLogLatest = data["[DCCMLOGLATEST]".Length..].Trim();
                    return;
                }
                else if (data.StartsWith("[DCCMDBG-CRASH]", StringComparison.Ordinal))
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
