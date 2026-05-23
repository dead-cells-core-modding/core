namespace ModCore
{
    public struct ContextConfig
    {
        private static bool is_locked = false;
        public static ContextConfig Default {
            get;
        } = new()
        {
            hashlinkLibraries = [
                "libhl",
                "hljit"
                ],
            consoleOutput = true
        };

        private static ContextConfig current = Default;

        internal static void SetReadonly()
        {
            is_locked = true;
        }

        public static ContextConfig Config {
            get {
                return current;
            }
            set {
                if (is_locked)
                {
                    throw new InvalidOperationException();
                }
                current = value;
            }
        }

        public bool disableWorkerProcessUtils;
        public string[] hashlinkLibraries;
        public Memory<byte>? hlbcOverride;
        public bool consoleOutput;
        public bool suppressFatalWindows;
    }
}
