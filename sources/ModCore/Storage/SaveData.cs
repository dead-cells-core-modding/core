using ModCore.Events;
using ModCore.Events.Interfaces.Game.Save;
using Newtonsoft.Json.Linq;

namespace ModCore.Storage
{
    /// <summary>
    /// Used to store data saved with game saves
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SaveData<T> : IEventReceiver,
        IOnBeforeSavingModdedSave,
        IOnAfterLoadingModdedSave
        where T : class, new()
    {
        int IEventReceiver.Priority => 0;

        /// <summary>
        /// The name of the SaveData
        /// </summary>
        public string Name
        {
            get;
        }

        /// <summary>
        /// The value of the SaveData
        /// </summary>
        public T Value
        {
            get; set;
        } = new();

        /// <summary>
        /// Create a SaveData
        /// </summary>
        /// <param name="name">The name of the SaveData, which should be unique</param>
        public SaveData(string name)
        {
            Name = name;

            EventSystem.AddReceiver(this);
        }

        void IOnBeforeSavingModdedSave.OnBeforeSavingModdedSave( Action<string, JObject> setData )
        {
            setData(Name, JObject.FromObject(Value));
        }

        void IOnAfterLoadingModdedSave.OnAfterLoadingModdedSave( Func<string, JObject?> getData )
        {
            Value = getData(Name)?.ToObject<T>() ?? new T();
        }
    }
}
