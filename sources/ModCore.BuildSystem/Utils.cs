using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.BuildSystem
{
    internal static class Utils
    {
        public static string CleanPath( string path )
        {
            if (path[0] == '\'' || path[0] == '"')
            {
                path = path[1..];
            }
            if (path[^1] == '\'' || path[^1] == '"')
            {
                path = path[..(path.Length - 1)];
            }
            return path;
        }
    }
}
