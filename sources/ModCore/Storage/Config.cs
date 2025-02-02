using ModCore.Events;
using ModCore.Events.Interfaces;
using Newtonsoft.Json;
using Serilog;


namespace ModCore.Storage
{
    public class Config<T> : IEventReceiver, IOnSaveConfig where T : new()
    {
        public int Priority => 0;

        public string ConfigName
        {
            get;
        }
        public string ConfigPath
        {
            get;
        }

        public JsonSerializerSettings SerializerOptions
        {
            get; set;
        } = new()
        {
            Formatting = Formatting.Indented
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

        public Config( string name )
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
                    value = JsonConvert.DeserializeObject<T>(File.ReadAllText(ConfigPath), SerializerOptions);
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
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(value, SerializerOptions));
            }
        }

        void IOnSaveConfig.OnSaveConfig()
        {
            Save();
        }
    }
}
