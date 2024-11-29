using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    public static class GameConstants
    {
        public static string ModCoreHostRoot { get; } = Path.GetDirectoryName(typeof(GameConstants).Assembly.Location)!;
        public static string ModCoreRoot { get; } = Path.GetFullPath(Path.Combine("../..", ModCoreHostRoot));
        public static string GameRoot { get; } = Path.GetDirectoryName(
            Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH") ??
            ModCoreRoot
            )!;
    }
}
