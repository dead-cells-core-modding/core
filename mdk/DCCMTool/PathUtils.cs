using System;
using System.Collections.Generic;
using System.Text;

namespace DCCMTool
{
    internal class PathUtils
    {
        public static string? GetDCCMRoot()
        {
            var root = Environment.GetEnvironmentVariable("DCCM_ROOT");
            if(Directory.Exists(root))
            {
                return Path.GetFullPath(root);
            }
            root = Path.Combine(Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH") ?? "", "coremod");
            if(Directory.Exists(root))
            {
                return Path.GetFullPath(root);
            }
            root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
            if (Directory.Exists(root))
            {
                return Path.GetFullPath(root);
            }
            return null;
        }
    }
}
