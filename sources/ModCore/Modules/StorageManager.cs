using ModCore.Modules.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ModCore.Modules
{
    [CoreModule]
    public class StorageManager : CoreModule<StorageManager>, IOnModCoreInjected
    {

        private class CacheMetadata
        {
            public Dictionary<string, CacheItem> Caches { get; set; } = [];
            public class CacheItem
            {

                public long LastModified { get; set; } = 0;
                
                public byte[] CacheChecksum { get; set; } = [];
                public byte[] Checksum { get; set; } = [];
            }
        }
        public override int Priority => ModulePriorities.Storage;

        private readonly string cacheMetadataPath = Path.Combine(GameConstants.CacheRoot, "cache.json");
        private CacheMetadata cache = new();

        void IOnModCoreInjected.OnModCoreInjected()
        {
            Directory.CreateDirectory(GameConstants.ModCoreRoot);
            Directory.CreateDirectory(GameConstants.ConfigRoot);
            Directory.CreateDirectory(GameConstants.CacheRoot);
            Directory.CreateDirectory(GameConstants.DataRoot);

            Logger.Information("Game Root: {root}", GameConstants.GameRoot);
            Logger.Information("Mod Core Root: {root}", GameConstants.ModCoreRoot);

            Logger.Information("Loading cache metadata");
            if(File.Exists(cacheMetadataPath))
            {
                try
                {
                    var data = JsonSerializer.Deserialize<CacheMetadata>(File.ReadAllText(cacheMetadataPath));
                    if(data != null)
                    {
                        cache = data;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to load cache metadata");
                }
            }
        }

        public string GetCachePath(string filename)
        {
            return Path.Combine(GameConstants.CacheRoot, filename);
        }
        public bool IsCacheOutdateOrMissing(string filename, long lastModified)
        {
            if(!cache.Caches.TryGetValue(filename, out var result))
            {
                return true;
            }
            var p = GetCachePath(filename);
            if (!File.Exists(p))
            {
                return true;
            }
            if(result.LastModified < lastModified)
            {
                return true;
            }
            using var fs = File.OpenRead(p);
            var hash = SHA256.HashData(fs);
            if(!Utils.MemCmp(hash, result.CacheChecksum))
            {
                return true;
            }

            return false;
        }
        public bool IsCacheOutdateOrMissing(string filename, byte[] checksum)
        {
            if (!cache.Caches.TryGetValue(filename, out var result))
            {
                return true;
            }
            var p = GetCachePath(filename);
            if (!File.Exists(p))
            {
                return true;
            }
            if (!Utils.MemCmp(result.CacheChecksum, checksum))
            {
                return true;
            }
            using var fs = File.OpenRead(p);
            var hash = SHA256.HashData(fs);
            if (!Utils.MemCmp(hash, result.CacheChecksum))
            {
                return true;
            }

            return false;
        }

        public void UpdateCache(string filename, long lastModified, ReadOnlySpan<byte> data)
        {
            cache.Caches[filename] = new()
            {
                CacheChecksum = SHA256.HashData(data),
                LastModified = lastModified,
            };
            using var fs = File.OpenWrite(filename);
            fs.Write(data);

            SaveCacheMetadata();
        }
        public void UpdateCache(string filename, byte[] checksum, ReadOnlySpan<byte> data)
        {
            cache.Caches[filename] = new()
            {
                CacheChecksum = SHA256.HashData(data),
                Checksum = checksum,
            };
            using var fs = File.OpenWrite(filename);
            fs.Write(data);

            SaveCacheMetadata();
        }

        public void UpdateCacheMetadata(string filename, long lastModified)
        {
            cache.Caches[filename] = new()
            {
                CacheChecksum = Utils.HashFile(GetCachePath(filename)),
                LastModified = lastModified,
            };
            SaveCacheMetadata();
        }
        public void UpdateCacheMetadata(string filename, byte[] checksum)
        {
            cache.Caches[filename] = new()
            {
                CacheChecksum = Utils.HashFile(GetCachePath(filename)),
                Checksum = checksum,
            };
            SaveCacheMetadata();
        }

        public void SaveCacheMetadata()
        {
            File.WriteAllText(cacheMetadataPath, JsonSerializer.Serialize(cache));
        }
    }
}
