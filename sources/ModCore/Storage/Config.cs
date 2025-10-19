using ModCore.Events;
using ModCore.Events.Interfaces;
using Newtonsoft.Json;
using Serilog;


namespace ModCore.Storage
{
    /// <summary>
    /// Configuration
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Config<T> : IEventReceiver, IOnSaveConfig where T : new()
    {
        /// <inheritdoc/>
        public int Priority => 0;

        /// <summary>
        /// The name of the configuration
        /// </summary>
        public string ConfigName
        {
            get;
        }

        /// <summary>
        /// The path to the configuration file
        /// </summary>
        public string ConfigPath
        {
            get;
        }

        /// <summary>
        /// The settings used when serializing the configuration file
        /// </summary>
        public JsonSerializerSettings SerializerOptions
        {
            get; set;
        } = new()
        {
            Formatting = Formatting.Indented
        };

        private T? value;

        /// <summary>
        /// The configured value
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The name of the configuration</param>

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

        /// <summary>
        /// Manually save the configuration
        /// </summary>
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
