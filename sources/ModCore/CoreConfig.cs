using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    internal class CoreConfig
    {
        public bool EnableDump { get; set; } = false;
        public bool AllowCloseConsole { get; set; } = false;
        public bool EnableGoldberg { get; set; } = true;
        public bool DetailedStackTrace { get; set; } = false;
    }
}
