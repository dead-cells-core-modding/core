using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    public static class ModulePriorities
    {
        public const int Game = -100;
        public const int HashlinkVM = -1000;

        public const int PlatformUtils = -990;

        public const int NativeHook = -1100;

        public const int HashlinkHook = -980;

        public const int Storage = -1200;
    }
}
