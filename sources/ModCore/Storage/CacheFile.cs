using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;


namespace ModCore.Storage
{
    /// <summary>
    /// Cache File
    /// </summary>
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

        /// <summary>
        /// Cache file storage location
        /// </summary>
        public string CachePath
        {
            get;
        }

        /// <summary>
        /// A value indicating whether the cache is valid
        /// </summary>
        public bool IsValid => isValid;

        private string MetadataPath
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The name of the cache file</param>
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

        /// <summary>
        /// Update the metadata of cached files
        /// </summary>
        /// <param name="name">The name of the metadata</param>
        /// <param name="data">Metadata content</param>
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

        /// <summary>
        /// Update the metadata of cached files
        /// </summary>
        /// <param name="name">The name of the metadata</param>
        /// <param name="data">Metadata content</param>
        public void UpdateMetadata( string name, string data )
        {
            UpdateMetadata( name, Encoding.UTF8.GetBytes(data) );
        }

        /// <summary>
        /// Update cache file
        /// </summary>
        /// <param name="data">Data</param>
        public void UpdateCache( ReadOnlySpan<byte> data )
        {
            File.WriteAllBytes(MetadataPath, data);

            UpdateCacheMetadata(SHA384.HashData(data));
        }

        /// <summary>
        /// Reports that the current cache file is valid
        /// </summary>
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

        /// <summary>
        /// Try to get the cache file contents.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A value indicating whether the cache is valid</returns>
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
