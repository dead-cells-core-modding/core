using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    public struct ContextConfig
    {
        private static bool is_locked = false;
        public static ContextConfig Default
        {
            get;
        } = new()
        {
            hashlinkLibraries = [
                "libhl",
                "hljit"
                ],
            slaveMode = false,
            consoleOutput = true
        };

        private static ContextConfig current = Default;

        internal static void SetReadonly()
        {
            is_locked = true;
        }

        public static ContextConfig Config
        {
            get
            {
                return current;
            }
            set
            {
                if (is_locked)
                {
                    throw new InvalidOperationException();
                }
                current = value;
            }
        }

        public string[] hashlinkLibraries;
        public Memory<byte>? hlbcOverride;
        public bool slaveMode;
        public bool consoleOutput;
    }
}
