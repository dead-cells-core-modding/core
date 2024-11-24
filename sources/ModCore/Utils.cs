using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    internal static class Utils
    {
        public static void ExitGame()
        {
            Process.GetCurrentProcess().Kill(true);
        }
    }
}
