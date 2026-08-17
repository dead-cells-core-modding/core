using Serilog;
using SteamLauncher.Platform;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;
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
            int parentPid)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT")))
            {
                Logger.Warning("DOTNET_ROOT is empty!");
            }

            Logger.Information("DOTNET_ROOT: {root}", Environment.GetEnvironmentVariable("DOTNET_ROOT"));

            Environment.SetEnvironmentVariable("DCCM_EXIT_WHEN_PROCESS_PID", parentPid.ToString());
            Environment.SetEnvironmentVariable("DEAD_CELLS_GAME_PATH", _gameRoot);

            Logger.Information("Starting game...");

            Process? game;
            var psi = PlatformServices.Current.ConfigureGameProcess(deadCellsExePath) ??
                new(deadCellsExePath);

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

            await game.WaitForExitAsync();

            return new LaunchResult
            {
                ExitCode = game.ExitCode,
            };
        }
    }
}
