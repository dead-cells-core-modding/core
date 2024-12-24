using ModCore.Events;
using ModCore.Modules.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ModCore.Storage
{
    public class Config<T> : IEventReceiver, IOnSaveConfig where T : new()
    {
        public int Priority => 0;

        public string ConfigName { get; }
        public string ConfigPath { get; }

        public JsonSerializerOptions SerializerOptions { get; set; } = new()
        {
            WriteIndented = true,
            IncludeFields = true,
            AllowTrailingCommas = true,
        };

        private T? value;
        public T Value
        {
            get
            {
                if (value == null)
                {
                    Load();
                }
                return value!;
            }
            set
            {
                this.value = value;
            }
        }

        public Config(string name)
        {
            ConfigName = name;
            ConfigPath = Path.Combine(FolderInfo.Config.FullPath, ConfigName + ".json");

            EventSystem.AddReceiver(this);
        }

        private void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    value = JsonSerializer.Deserialize<T>(File.ReadAllText(ConfigPath), SerializerOptions);
                }
                else
                {
                    value = new();
                    Save();
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to load config {name}", ConfigName);
                value = new();
            }
        }

        public void Save()
        {
            if (value != null)
            {
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(value, SerializerOptions));
            }
        }

        void IOnSaveConfig.OnSaveConfig()
        {
            Save();
        }
    }
}
