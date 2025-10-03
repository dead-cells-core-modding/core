using ModCore.Storage;
using Newtonsoft.Json;

namespace ModCore
{
    internal class CoreConfig
    {
        [JsonIgnore]
        public bool NoConsole { get; set; } = false;
        public bool GeneratePseudocodeAssembly { get; set; } = false;
        public bool AllowCloseConsole { get; set; } = false;
        // Enabled by default for non-Steam platforms
        public bool EnableGoldberg { get; set; } = !File.Exists(FolderInfo.GameRoot.GetFilePath("steam.hdll"));
        public bool SkipLogoSplash { get; set; } = true;
    }
}
