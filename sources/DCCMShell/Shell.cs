using ModCore;
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
            Startup.StartGame();
        }
        public static void StartFromNative( IntPtr args, int sizeBytes )
        {
            NativeArgs* pargs = (NativeArgs*)args;
            try
            {
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
