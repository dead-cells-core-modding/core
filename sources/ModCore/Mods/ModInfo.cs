﻿using ModCore.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CS1591

namespace ModCore.Mods
{
    /// <summary>
    /// Indicates mod information
    /// </summary>
    public class ModInfo
    {
        [JsonProperty("name")]
        public required string Name { get; set; }
        [JsonProperty("version")]
        public required string Version { get; set; }
        [JsonProperty("type")]
        public required string Type { get; set; }
        [JsonProperty("dependencies")]
        public List<string> Dependencies { get; set; } = [];
        [JsonProperty("repositoryUrl")]
        public string RepositoryUrl { get; set; } = "";
        [JsonProperty("license")]
        public string License { get; set; } = "";

        [JsonIgnore]
        public FolderInfo? ModRoot { get; set; }
    }
}
