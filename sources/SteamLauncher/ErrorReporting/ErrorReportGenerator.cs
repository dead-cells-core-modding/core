using Serilog;
using SteamLauncher.Platform;
using System.Diagnostics;
using System.Text;

namespace SteamLauncher.ErrorReporting
{
    /// <summary>
    /// Error report generator——collects crash information and generates the last_error.txt report file
    /// </summary>
    internal class ErrorReportGenerator
    {
        private static ILogger Logger => Log.Logger;

        public static readonly string ERROR_REPORT_PATH =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_error.txt");

        public static readonly string ERROR_REPORT_HEADER = $"""

A Fatal Error / Unhandled Exception has occurred and the game has crashed.

When reporting the issue, please include:

- Crash logs ({ERROR_REPORT_PATH})
- Reproduction steps
- System and version information
- Crash Dump (if available)

You can report or contact us via:

- Discord: https://discord.gg/9euFADqXEC
- Github Issue: https://github.com/dead-cells-core-modding/core/issues/new/choose

""";

        /// <summary>
        /// Generate an error report and write it to last_error.txt
        /// </summary>
        /// <param name="exitCode">Game process exit code</param>
        /// <param name="errorOutput">Standard error output from the game process</param>
        /// <param name="outputData">Standard output from the game process (in diagnostic mode)</param>
        /// <param name="gameLogPath">Game log file path</param>
        /// <param name="crashDumpPath">Crash dump file path</param>
        public void GenerateReport(
            int exitCode,
            bool printEnv,
            string? errorOutput,
            string? outputData,
            string? gameLogPath,
            string? crashDumpPath,
            string? launcherLogPath)
        {
            Logger.Error(ERROR_REPORT_HEADER);
            Logger.Information("Please check the log file for more detailed information: {path}", ERROR_REPORT_PATH);

            var errText = new StringBuilder();

            // Detect Wine/Proton compatibility layer
            {
                var wineVer = PlatformServices.Current.DetectCompatibilityLayer();
                if (!string.IsNullOrEmpty(wineVer))
                {
                    errText.AppendLine();
                    errText.Append("The current program is running on Proton/Wine: ");
                    errText.AppendLine(wineVer);
                    errText.AppendLine();
                }
            }

            errText.AppendLine();
            errText.Append("Exit code: ");
            errText.Append(exitCode);
            errText.Append(" (0x");
            errText.Append(exitCode.ToString("X"));
            errText.AppendLine(")");

            errText.AppendLine("\n:============ SYSTEM SPECIFICATIONS ============:\n");

            try
            {
                errText.Append(PlatformServices.Current.GetSystemSpec());
            }
            catch (Exception ex)
            {
                errText.AppendLine("[ERROR] Failed to collect system specifications:");
                errText.AppendLine(ex.Message);
            }

            if (!string.IsNullOrEmpty(errorOutput))
            {
                errText.AppendLine("\n:============ ERRORS ============:\n");
                errText.AppendLine(errorOutput);
                errText.AppendLine();
            }

            if (!string.IsNullOrEmpty(launcherLogPath) && File.Exists(launcherLogPath))
            {
                errText.AppendLine("\n:============ LAUNCHER LOG ============:\n");
                errText.AppendLine(File.ReadAllText(launcherLogPath));
                errText.AppendLine();
            }

            if (!string.IsNullOrEmpty(gameLogPath) && File.Exists(gameLogPath))
            {
                errText.AppendLine("\n:============ GAME LOG ============:\n");
                errText.AppendLine(File.ReadAllText(gameLogPath));
                errText.AppendLine();
            }

            if (!string.IsNullOrEmpty(outputData))
            {
                errText.AppendLine("\n:============ FULL OUTPUT ============:\n");
                errText.AppendLine(outputData);
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
        }
    }
}
