using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ModCore.Storage
{
    public class CacheFile
    {
        private class Metadata
        {
            public byte[] cacheFileHash = [];
            public Dictionary<string, byte[]> metadata = [];
        }

        private readonly bool debug_invalid_cache = 
            Environment.GetEnvironmentVariable("DCCM_DEBUG_OPTION_CACHE_INVALID") == "true";
        private bool isValid;
        private readonly Metadata metadata;
        public string CachePath
        {
            get;
        }

        public bool IsValid => isValid;

        private string MetadataPath
        {
            get;
        }

        public CacheFile( string name )
        {
            CachePath = Path.Combine(FolderInfo.Cache.FullPath, name);
            MetadataPath = Path.Combine(FolderInfo.Cache.FullPath, name + ".cache");

            if (!File.Exists(MetadataPath) || debug_invalid_cache)
            {
                metadata = new();
            }
            else
            {
                metadata = JsonConvert.DeserializeObject<Metadata>(File.ReadAllText(MetadataPath)) ?? new();
                if (File.Exists(CachePath))
                {
                    var hash = SHA384.HashData(File.ReadAllBytes(CachePath));
                    if (new ReadOnlySpan<byte>(hash).SequenceEqual(metadata.cacheFileHash))
                    {
                        isValid = true;
                    }
                }
            }

        }

        private void SaveMetadata()
        {
            File.WriteAllText(MetadataPath, JsonConvert.SerializeObject(metadata));
        }

        public void UpdateMetadata( string name, ReadOnlySpan<byte> data )
        {
            if (data.Length > 384)
            {
                data = SHA384.HashData(data);
            }
            if (metadata.metadata.TryGetValue(name, out var d))
            {
                if (!data.SequenceEqual(d))
                {
                    isValid = false;
                }
            }
            else
            {
                isValid = false;
            }
            metadata.metadata[name] = data.ToArray();
        }
        public void UpdateMetadata( string name, string data )
        {
            UpdateMetadata( name, Encoding.UTF8.GetBytes(data) );
        }
        public void UpdateCache( ReadOnlySpan<byte> data )
        {
            File.WriteAllBytes(MetadataPath, data);

            UpdateCacheMetadata(SHA384.HashData(data));
        }
        public void UpdateCache()
        {
            using var fs = File.OpenRead(CachePath);
            UpdateCacheMetadata(
                SHA384.HashData(fs)
                );
        }
        private void UpdateCacheMetadata( byte[] hash )
        {
            metadata.cacheFileHash = hash;
            SaveMetadata();
            isValid = true;
        }
        public bool TryGetCache( out ReadOnlySpan<byte> data )
        {
            if (!isValid)
            {
                data = default;
                return false;
            }
            if (!File.Exists(CachePath))
            {
                isValid = false;
                data = default;
                return false;
            }
            data = File.ReadAllBytes(CachePath);
            return true;
        }
    }
}
