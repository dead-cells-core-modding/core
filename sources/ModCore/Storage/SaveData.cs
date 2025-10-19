using dc;
using dc.spine;
using dc.tool;
using ModCore.Events;
using ModCore.Events.Interfaces.Game.Save;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Storage
{
    /// <summary>
    /// Used to store data saved with game saves
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SaveData<T> : IEventReceiver,
        IOnCopySave,
        IOnDeleteSave,
        IOnAfterLoadingSave,
        IOnAfterSavingSave
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

        /// <summary>
        /// Get the data file path
        /// </summary>
        /// <param name="slot">Storage slot id, defaults to the currently active game save</param>
        /// <returns></returns>
        public string GetSavePath( int? slot )
        {
            var name = Save.Class.fileName(slot).ToString();

            return FolderInfo.SaveRoot.GetFilePath(System.IO.Path.ChangeExtension(name, Name + ".mod.json"));
        }

        void IOnCopySave.OnCopySave( IOnCopySave.EventData data )
        {
            var from = GetSavePath(data.SlotFrom);
            var to = GetSavePath(data.SlotTo);
            if (!System.IO.File.Exists(from))
            {
                return;
            }
            System.IO.File.Copy(from, to, true);
        }

        void IOnDeleteSave.OnDeleteSave( int? slot )
        {
            var to = GetSavePath(slot);
            if (!System.IO.File.Exists(to))
            {
                return;
            }
            System.IO.File.Delete(to);
        }

        void IOnAfterLoadingSave.OnAfterLoadingSave( User data )
        {
            var to = GetSavePath(null);
            T? value = null;
            if (System.IO.File.Exists(to))
            {
                value = JsonConvert.DeserializeObject<T>(System.IO.File.ReadAllText(to));
            }
            value ??= new();
            Value = value;
        }

        void IOnAfterSavingSave.OnAfterSavingSave()
        {
            var to = GetSavePath(null);
            System.IO.File.WriteAllText(to, 
                JsonConvert.SerializeObject(Value)
                );
        }
    }
}
