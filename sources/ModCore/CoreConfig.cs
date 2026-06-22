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
        public bool UseGameCDBManager { get; set; } = true;
        public bool AllowLockCursor { get; set; } = true;
        // Enabled by default when neither Steam nor GOG native libs are present
        public bool EnableGoldberg { get; set; } = !File.Exists(FolderInfo.GameRoot.GetFilePath("steam.hdll")) &&
                                                    !File.Exists(FolderInfo.GameRoot.GetFilePath("gog.hdll"));
        public bool SkipLogoSplash { get; set; } = true;
    }
}
