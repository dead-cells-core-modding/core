using Newtonsoft.Json;

namespace ModCore
{
    internal class CoreConfig
    {
        [JsonIgnore]
        public bool NoConsole { get; set; } = false;
        public bool EnableDump { get; set; } = false;
        public bool AllowCloseConsole { get; set; } = false;
        public bool EnableGoldberg { get; set; } = true;
        public bool DetailedStackTrace { get; set; } = false;
        public bool SkipLogoSplash { get; set; } = true;
    }
}
