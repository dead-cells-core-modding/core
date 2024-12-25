using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeadCellsModding
{
    internal static class Program
    {
        private static string CombineModCore(string root)
        {
            return Path.Combine(root, "coremod", "core", "host", "ModCore.dll");
        }
        private static void Main(string[] args)
        {
            var gameRoot = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH");
            if(string.IsNullOrEmpty(gameRoot))
            {
                gameRoot = Path.GetDirectoryName(Environment.ProcessPath!)!;
            }
            string? modcore = null;
            gameRoot = Path.GetFullPath(gameRoot);
            while (!string.IsNullOrEmpty(gameRoot))
            {
                modcore = CombineModCore(gameRoot);
                Console.WriteLine("Try find ModCore in " + modcore);
                if (File.Exists(modcore))
                {
                    break;
                }
                gameRoot = Path.GetDirectoryName(gameRoot);
            }

            if (modcore == null || !File.Exists(modcore))
            {
                throw new FileNotFoundException(null, "ModCore.dll");
            }

            Directory.SetCurrentDirectory(gameRoot!);

            var core = Assembly.LoadFrom(modcore);
            var startup = core.GetType("ModCore.Startup");
            startup!.GetMethod("StartGame")!.Invoke(null, null);
        }
    }
}
