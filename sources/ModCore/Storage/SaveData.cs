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
    public class SaveData<T> : IEventReceiver,
        IOnCopySave,
        IOnDeleteSave,
        IOnAfterLoadingSave,
        IOnAfterSavingSave
        where T : class, new()
    {
        int IEventReceiver.Priority => 0;

        public string Name
        {
            get;
        }

        public T Value
        {
            get; set;
        } = new();

        public SaveData(string name)
        {
            Name = name;

            EventSystem.AddReceiver(this);
        }

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
