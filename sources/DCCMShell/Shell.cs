using ModCore;

namespace DCCMShell
{
    public static class Shell
    {
        public static void StartFromShell()
        {
            Startup.StartGame();
        }
        public static void StartFromNative( IntPtr args, int sizeBytes )
        {
            Startup.StartGame();
        }
        public static void Main( string[] args )
        {
            Startup.StartGame();
        }
    }
}
