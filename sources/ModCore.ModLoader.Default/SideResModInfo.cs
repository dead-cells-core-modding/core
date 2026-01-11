using ModCore.Mods;
using Newtonsoft.Json;

namespace ModCore.ModLoader.Default
{
    public class SideResModInfo : ModInfo
    {
        [JsonProperty("paks")]
        public required List<string> Paks { 
            get; set; 
        }
        [JsonProperty("workshop")]
        public bool Workshop
        {
            get; set;
        }
    }
}
