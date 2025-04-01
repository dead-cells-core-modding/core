namespace ModCore
{
    internal class CoreConfig
    {
        public bool EnableDump { get; set; } = false;
        public bool AllowCloseConsole { get; set; } = false;
        public bool EnableGoldberg { get; set; } = true;
        public bool DetailedStackTrace { get; set; } = false;
        public bool SkipLogoSplash { get; set; } = true;
    }
}
