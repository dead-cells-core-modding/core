using ModCore.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModCore.Mods
{
    public class ModInfo
    {
        [JsonProperty("name")]
        public required string Name { get; set; }
        [JsonProperty("version")]
        public required string Version { get; set; }
        [JsonProperty("type")]
        public required string Type {  get; set; }

        [JsonIgnore]
        public FolderInfo? ModRoot { get; set; }
    }
}
