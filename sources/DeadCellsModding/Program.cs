using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeadCellsModding
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var gameRoot = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH");
            if(string.IsNullOrEmpty(gameRoot))
            {
                gameRoot = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
            }
            var core = Assembly.LoadFile(Path.Combine(gameRoot, "coremod", "core", "host", "net8.0", "ModCore.dll"));
            var startup = core.GetType("ModCore.Startup");
            startup!.GetMethod("StartGame")!.Invoke(null, null);
        }
    }
}
