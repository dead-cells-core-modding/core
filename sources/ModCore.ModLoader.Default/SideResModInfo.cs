using ModCore.Mods;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
