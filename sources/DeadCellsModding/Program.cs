using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DeadCellsModding
{
    internal static class Program
    {
        private static string CombineModCore( string root )
        {
            return Path.Combine(root, "coremod", "core", "host", "DCCMShell.dll");
        }
        private static void StartGame()
        {
            var gameRoot = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH");
            if (string.IsNullOrEmpty(gameRoot))
            {
                gameRoot = Path.GetDirectoryName(Environment.ProcessPath!)!;
            }

            var steamid = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath!)!, "steam_appid.txt");
            if (!File.Exists(steamid))
            {
                try
                {
                    File.WriteAllText(steamid, "588650");
                }
                catch (Exception)
                {
                }
            }

            string? modcore = null;
            gameRoot = Path.GetFullPath(gameRoot);
            while (!string.IsNullOrEmpty(gameRoot))
            {
                modcore = CombineModCore(gameRoot);
#if DEBUG
                Console.WriteLine("Try find ModCore in " + modcore);
#endif
                if (File.Exists(modcore))
                {
                    break;
                }
                gameRoot = Path.GetDirectoryName(gameRoot);
            }

            if (modcore == null || !File.Exists(modcore))
            {
                throw new FileNotFoundException(null, "DCCMShell.dll");
            }

            Directory.SetCurrentDirectory(gameRoot!);

            var core = Assembly.LoadFrom(modcore);
            var startup = core.GetType("DCCMShell.Shell");
            startup!.GetMethod("StartFromShell")!.CreateDelegate<Action>()();
        }
        private static void Main( string[] args )
        {
            StartGame();
        }
    }
}
