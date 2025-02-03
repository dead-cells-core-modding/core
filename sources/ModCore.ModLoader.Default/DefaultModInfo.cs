using ModCore.Mods;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.ModLoader.Default
{
    public class DefaultModInfo : ModInfo
    {
        [JsonProperty("main")]
        public string MainModType
        {
            get; set;
        } = "";
        [JsonProperty("assemblies")]
        public required List<string> Assemblies
        {
            get; set;
        }

        [JsonIgnore]
        public List<Assembly> LoadedAssemblies { get; } = [];
    }
}
