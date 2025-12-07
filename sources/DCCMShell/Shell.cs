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
        public static void StartFromShell()
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
            Startup.StartGame();
        }
    }
}
