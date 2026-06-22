using Serilog;
using System.Diagnostics;
using System.Text;

namespace GOGStartShell
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--update")
            {
                var pid = int.Parse(args[1]);
                var dst = args[2];
                try { Process.GetProcessById(pid).WaitForExit(); } catch { }
                File.Copy(Environment.ProcessPath!, dst, true);
                Process.Start(dst);
                return 0;
            }

            InitLog();
            var logger = Log.Logger;

            string gameRoot = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH")!;
            if (string.IsNullOrEmpty(gameRoot))
                gameRoot = Path.GetDirectoryName(Environment.ProcessPath!)!;

            gameRoot = Path.GetFullPath(gameRoot);
            while (!string.IsNullOrEmpty(gameRoot))
            {
                var glExe = Path.GetFullPath(Path.Combine(gameRoot, "deadcells_gl.exe"));
                if (File.Exists(glExe) && Environment.ProcessPath != glExe)
                    break;
                gameRoot = Path.GetDirectoryName(gameRoot)!;
            }

            if (string.IsNullOrEmpty(gameRoot))
            {
                logger.Error("Game directory not found.");
                return -1;
            }

            Directory.SetCurrentDirectory(gameRoot);

            bool diagnosticMode = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diagnostic_mode.txt"));

            Environment.SetEnvironmentVariable("DEAD_CELLS_GAME_PATH", gameRoot);
            Environment.SetEnvironmentVariable("DCCM_EXIT_WHEN_PROCESS_PID", Environment.ProcessId.ToString());

            if (diagnosticMode)
            {
                Environment.SetEnvironmentVariable("DCCM_DIAGNOSTIC_MODE", "true");
                logger.Warning("Diagnostic mode enabled.");
            }

            if (bool.TryParse(Environment.GetEnvironmentVariable("DCCM_SHOULD_WAIT_FOR_DEBUGGER"), out var wait) && wait)
            {
                logger.Information("Waiting for debugger...");
                Debugger.Launch();
            }

            var deadCellsExePath = Path.Combine(gameRoot, "coremod", "core", "host", "startup", "DeadCellsModding");

            logger.Information("Starting game from {root}", gameRoot);

            var psi = new ProcessStartInfo(deadCellsExePath)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = diagnosticMode,
            };

            Process game;
            try
            {
                game = Process.Start(psi)!;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to start game. Try deleting {path} to reset DCCM.",
                    Path.GetFullPath(Path.Combine(gameRoot, "coremod")));
                await Task.Delay(5000);
                return -1;
            }

            var errOutput = new StringBuilder();
            string? gameLogPath = null;
            string? crashDumpPath = null;

            game.ErrorDataReceived += (_, ev) =>
            {
                var data = ev.Data ?? "";
                if (data.StartsWith("[DCCMLOGLATEST]"))
                    gameLogPath = data["[DCCMLOGLATEST]".Length..].Trim();
                else if (data.StartsWith("[DCCMDBG-CRASH]"))
                    crashDumpPath = data["[DCCMDBG-CRASH]".Length..].Trim();
                else
                    errOutput.AppendLine(data);
            };
            game.BeginErrorReadLine();

            if (diagnosticMode)
            {
                game.OutputDataReceived += (_, ev) => Console.WriteLine(ev.Data);
                game.BeginOutputReadLine();
            }

            await game.WaitForExitAsync();

            if (game.ExitCode != 0)
            {
                logger.Error("Game exited with non-zero code {code}", game.ExitCode);
                if (gameLogPath != null)
                    logger.Error("Game log: {path}", gameLogPath);
                if (crashDumpPath != null)
                    logger.Error("Crash dump: {path}", crashDumpPath);
                if (errOutput.Length > 0)
                    logger.Error("stderr:\n{err}", errOutput);
                await Task.Delay(5000);
            }

            return game.ExitCode;
        }

        static void InitLog()
        {
            var logPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath!)!, "log_latest.log");
            try { File.Delete(logPath); } catch { }
            const string fmt = "[{Timestamp:HH:mm:ss} {Level:u3}][GOGShell] {Message:lj}{NewLine}{Exception}";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, outputTemplate: fmt)
                .WriteTo.Console(outputTemplate: fmt)
                .CreateLogger();
        }
    }
}
