using ModCore;
using ModCore.Storage;
using ModCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace DCCMShell.Reporting
{
    [SupportedOSPlatform("windows5.2")]
    internal partial class ErrorReporter
    {
        [LibraryImport("ucrtbase", SetLastError = true)]
        private static partial int _open_osfhandle( nint osfhandle, int flags );
        [LibraryImport("ucrtbase", SetLastError = true)]
        private static partial int _dup2( int fd1, int fd2 );
        [LibraryImport("ucrtbase")]
        private static partial int _flushall();

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


            ContextConfig.Config = ContextConfig.Config with
            {
                redirectError = true
            };

            Console.SetError(new StreamWriter(pipeServer, Encoding.Unicode)
            {
                AutoFlush = true,
            });

            var hPipe = pipeServer.SafePipeHandle.DangerousGetHandle();

            SetStdHandle(Windows.Win32.System.Console.STD_HANDLE.STD_ERROR_HANDLE, new HANDLE(hPipe));

            _flushall();
            var fd = _open_osfhandle(hPipe, 0x8000);
            _dup2(fd, 2);

        }
        public static unsafe void ReporterMain()
        {
            var parent = int.Parse(Environment.GetEnvironmentVariable("DCCM_ERROR_REPORTER_PARENT")!);
            var proc = Process.GetProcessById(parent);

            var pipeHandle = Environment.GetEnvironmentVariable("DCCM_ERROR_REPORTER_PIPE")!;

            pipeClient = new(PipeDirection.In, pipeHandle);

            StringBuilder err = new();
            byte[] buffer = new byte[8192];

            while (!proc.HasExited)
            {
                var count = pipeClient.Read(buffer);
                var str = Encoding.UTF8.GetString(buffer[..count]);
                err.Append(str);

                Thread.Sleep(1);
            }

            proc.WaitForExit();

            var hProc = OpenProcess(Windows.Win32.System.Threading.PROCESS_ACCESS_RIGHTS.PROCESS_SYNCHRONIZE |
                Windows.Win32.System.Threading.PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, true, (uint)parent);

            uint exitCode = 0;
            GetExitCodeProcess(hProc, &exitCode);
            if (exitCode != 0)
            {
                new ErrorReportGenerator().GenerateReport((int)exitCode,
                    false, err.ToString(),
                    null, Environment.GetEnvironmentVariable("DCCM_ERROR_LATEST_LOG"),
                    null, null
                    );
            }
        }
    }
}
