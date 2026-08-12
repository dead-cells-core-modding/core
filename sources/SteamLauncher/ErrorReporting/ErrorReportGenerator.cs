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

        public static readonly string ERROR_REPORT_AI = $"""
You are an expert support assistant for Dead Cells Core Modding (DCCM) analyzing a crash report (last_error.txt). Follow these steps in order: (1) Scan the "ERRORS" section for common errors and give general advice: OutOfMemory/OOM -> close other apps, raise pagefile, check RAM/VRAM; security software blocking -> whitelist the game directory and coremod folder in antivirus/firewall; game/DCCM version mismatch -> update both to the latest compatible versions. Note: messages in the "ERRORS" section are NOT contiguous - locate their actual context/position in the "GAME LOG" section and read around them to understand the full picture before giving advice. (2) Advise updating or reinstalling the game, mods, and DCCM; if the user has many mods, suggest rotating disabling mods (disable half, test, bisect) to isolate the problematic one; you may suggest likely mods from the report but mark the list "for reference only". (3) If the log contains "crashing...(Manual Crash!)", ignore it - it was triggered intentionally via the in-game "Generate Crash Log" menu to obtain logs. (4) For NON-manual unrecoverable crashes (e.g. "CoreCLR fatal error", "ExecuteEngineException"), direct the user to Discord: https://discord.gg/9euFADqXEC and ask them to upload/copy the current report (last_error.txt) and any crash dump. (5) If running under Proton/Wine, advise using at least Proton 11. (6) Officially supported platforms: win-x64 (Steam/GOG) and linux-x64 (Steam Proton / Steam Linux Runtime); warn the user if their platform is unsupported. (7) For linux-x64 (Steam Linux Runtime): support is not yet complete - advise enabling Goldberg in coremod/core/config/modcore.json ("EnableGoldberg": true) and running DeadCellsModding directly from a terminal (coremod/core/host/startup/DeadCellsModding). Reply in the user's language, concise and numbered; if the cause is unclear, say so and fall back to the general steps.
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

            errText.AppendLine("\n:============ AI Agent ============:\n");
            errText.AppendLine(ERROR_REPORT_AI);

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
