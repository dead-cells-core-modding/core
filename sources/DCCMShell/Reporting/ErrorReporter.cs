using ModCore;
using ModCore.Modules;
using ModCore.Storage;
using ModCore.Utilities;
using Serilog;
using SteamLauncher.ErrorReporting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;


namespace DCCMShell.Reporting
{
    internal partial class ErrorReporter
    {
        private static AnonymousPipeServerStream? pipeServer;
        private static AnonymousPipeClientStream? pipeClient;
        public static void SetupErrorReporter()
        {
            Console.WriteLine("Setup error reporting");

            var latest = Path.Combine(FolderInfo.Logs.FullPath, "log_latest.log");

            pipeServer = new(PipeDirection.Out, HandleInheritability.Inheritable);

            WorkerProcessUtils.StartWorkerProcess(typeof(ErrorReporter).AssemblyQualifiedName!, nameof(ReporterMain), new()
            {
                Environment =
                {
                    ["DCCM_ERROR_REPORTER_PARENT"] = Environment.ProcessId.ToString(),
                    ["DCCM_ERROR_REPORTER_PIPE"] = pipeServer.GetClientHandleAsString(),
                    ["DCCM_ERROR_LATEST_LOG"] = latest
                }
            }, false);

            pipeServer.DisposeLocalCopyOfClientHandle();

            // Redirect

            var writer = new StreamWriter(pipeServer, Encoding.UTF8)
            {
                AutoFlush = true,
            };

            ContextConfig.Config = ContextConfig.Config with
            {
                redirectError = false,
                configurateLogger = conf =>
                {
                    conf.WriteTo.TextWriter(writer, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
                        outputTemplate: LogInitializer.OUTPUT_FORMAT_TEMPLATE);
                }
            };

            Console.SetError(writer);

            var hPipe = pipeServer.SafePipeHandle.DangerousGetHandle();

            Platform.PlatformServices.Current.RedirectStderr(hPipe);

        }
        public static void ReporterMain()
        {
            var parent = int.Parse(Environment.GetEnvironmentVariable("DCCM_ERROR_REPORTER_PARENT")!);
            var proc = Process.GetProcessById(parent);

            var pipeHandle = Environment.GetEnvironmentVariable("DCCM_ERROR_REPORTER_PIPE")!;

            pipeClient = new(PipeDirection.In, pipeHandle);

            StringBuilder err = new();
            byte[] buffer = new byte[8192];

            while (true)
            {
                var count = pipeClient.Read(buffer);

                if (count == 0 && proc.HasExited)
                {
                    break;
                }

                var str = Encoding.UTF8.GetString(buffer[..count]);
                err.Append(str);

                Thread.Sleep(1);
            }

            proc.WaitForExit();

            var errText = err.ToString();

            var exitCode = Platform.PlatformServices.Current.GetExitCode(parent);
            if (exitCode != 0)
            {
                new ErrorReportGenerator().GenerateReport(exitCode,
                    false, errText,
                    null, Environment.GetEnvironmentVariable("DCCM_ERROR_LATEST_LOG"),
                    null, null
                    );
            }
        }
    }
}
