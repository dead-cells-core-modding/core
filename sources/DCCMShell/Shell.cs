using ModCore;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DCCMShell
{
    public unsafe static partial class Shell
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct ManagedAPIInfo
        {
            public int count;
            public char** names;
            public void*** ptr;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeArgs
        {
            public char** err;
            public int argc;
            public void** args;
            public ManagedAPIInfo* api_info;
        }
        public static void StartFromShell( nint _, int _1 )
        {
            var err = Startup.CheckEnv(out var errMsg);
            if(err != Startup.CheckEnvResult.Success)
            {
                if (err == Startup.CheckEnvResult.DotnetVersionTooLow)
                {
                    Console.Error.WriteLine($"DCCM requires .NET 10 or higher.If you see this prompt repeatedly, try updating {Path.GetFileName(Environment.ProcessPath)}.");
                }
                else
                {
                    Console.Error.WriteLine(err + ":" + errMsg);
                }
                Thread.Sleep(TimeSpan.FromSeconds(5));
                Environment.Exit(-1);
            }

            if (bool.TryParse(Environment.GetEnvironmentVariable("DCCM_SHOULD_WAIT_FOR_DEBUGGER"), out var shouldWaitForDebugger) && 
                shouldWaitForDebugger)
            {
                Console.WriteLine("Waiting for debugger attach...");
                Debugger.Launch();
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("DCCM_EXIT_WHEN_PROCESS_PID"), out var parentPID))
            {
                try
                {
                    var proc = Process.GetProcessById(parentPID);
                   
                   
                    new Thread(_ =>
                    {
                        Console.WriteLine("Attaching to parent process: " + parentPID);
                        while (true)
                        {
                            proc.Refresh();
                            if (proc.HasExited)
                            {
                                Console.WriteLine("Parent process exited");
                                Environment.Exit(0);
                            }
                            Thread.Sleep(100);
                        }
                    })
                    {
                        IsBackground = true
                    }.Start();
                } catch 
                {
                }
            }

            Startup.StartGame();
        }

        public static void StartFromNative( IntPtr args, int sizeBytes )
        {
            NativeArgs* pargs = (NativeArgs*)args;
            try
            {
                if (Startup.CheckEnv(out string? err) != Startup.CheckEnvResult.Success)
                {
                    Console.Error.WriteLine(err);
                    *pargs->err = (char*)Marshal.StringToHGlobalAnsi(err);
                    return;
                }
                InitializeManagedAPIs(pargs->api_info);
                Startup.StartGame();
            }
            catch (Exception ex)
            {
                *pargs->err = (char*)Marshal.StringToHGlobalAnsi(ex.ToString());
            }
        }
        public static void Main( string[] args )
        {
            StartFromShell(0, 0);
        }
    }
}
