using Serilog;

namespace ModCore
{
    /// <summary>
    /// Configures how the framework initializes the Hashlink runtime and its surrounding services.
    /// </summary>
    public struct ContextConfig
    {
        private static bool is_locked = false;
        /// <summary>
        /// Gets the default context configuration.
        /// </summary>
        public static ContextConfig Default {
            get;
        } = new()
        {
            hashlinkLibraries = [
                "libhl",
                "hljit"
                ],
            consoleOutput = true,
            dontStartAsDeadCells = false
        };

        private static ContextConfig current = Default;

        internal static void SetReadonly()
        {
            is_locked = true;
        }

        /// <summary>
        /// Gets or sets the active context configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration has been locked.</exception>
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

        /// <summary>
        /// An optional callback used to configure the logger.
        /// </summary>
        public Action<LoggerConfiguration>? configurateLogger;

        /// <summary>
        /// Indicates whether worker process utilities are disabled.
        /// </summary>
        public bool disableWorkerProcessUtils;
        /// <summary>
        /// The names of the Hashlink native libraries to load.
        /// </summary>
        public string[] hashlinkLibraries;
        /// <summary>
        /// An optional override for the Hashlink bytecode.
        /// </summary>
        public Memory<byte>? hlbcOverride;
        /// <summary>
        /// Indicates whether log output is written to the console.
        /// </summary>
        public bool consoleOutput;
        /// <summary>
        /// Indicates whether error output is redirected.
        /// </summary>
        public bool redirectError;
        /// <summary>
        /// Indicates whether fatal error windows are suppressed.
        /// </summary>
        public bool suppressFatalWindows;
        /// <summary>
        /// Indicates whether mod loading is disabled.
        /// </summary>
        public bool noModsMode;
        /// <summary>
        /// Indicates whether the game should not start as Dead Cells.
        /// </summary>
        public bool dontStartAsDeadCells;
    }
}
