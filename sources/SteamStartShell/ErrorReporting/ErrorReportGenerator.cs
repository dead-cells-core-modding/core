using Serilog;
using SteamStartShell.Platform;
using System.Diagnostics;
using System.Text;

namespace SteamStartShell.ErrorReporting
{
    /// <summary>
    /// 错误报告生成器——收集崩溃信息并生成 last_error.txt 报告文件
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

- Discord: https://discord.gg/qH5gw7hwx7

""";

        /// <summary>
        /// 生成错误报告并写入 last_error.txt
        /// </summary>
        /// <param name="exitCode">游戏进程退出码</param>
        /// <param name="errorOutput">游戏进程的标准错误输出</param>
        /// <param name="outputData">游戏进程的标准输出（诊断模式下）</param>
        /// <param name="gameLogPath">游戏日志文件路径</param>
        /// <param name="crashDumpPath">崩溃转储文件路径</param>
        public void GenerateReport(
            int exitCode,
            string? errorOutput,
            string? outputData,
            string? gameLogPath,
            string? crashDumpPath )
        {
            Logger.Error(ERROR_REPORT_HEADER);
            Logger.Information("Please check the log file for more detailed information: {path}", ERROR_REPORT_PATH);

            var errText = new StringBuilder();

            // 检测 Wine/Proton 兼容层
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

            if (!string.IsNullOrEmpty(errorOutput))
            {
                errText.AppendLine("\n:============ BELOW IS THE ERRORS ============:\n");
                errText.AppendLine(errorOutput);
                errText.AppendLine();
            }

            if (!string.IsNullOrEmpty(gameLogPath) && File.Exists(gameLogPath))
            {
                errText.AppendLine("\n:============ BELOW IS THE GAME LOG ============:\n");
                errText.AppendLine(File.ReadAllText(gameLogPath));
                errText.AppendLine();
            }

            if (!string.IsNullOrEmpty(outputData))
            {
                errText.AppendLine("\n:============ BELOW IS FULL OUTPUT ============:\n");
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
