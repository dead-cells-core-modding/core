using dc;
using dc.tool;
using ModCore.Events;
using ModCore.Events.Interfaces.Game.Save;
using ModCore.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    internal class SaveManager : CoreModule<SaveManager>,
        IOnCopySave,
        IOnDeleteSave,
        IOnAfterLoadingSave,
        IOnAfterSavingSave
    {
        private JObject? currentSave;

        public static string GetModdedSavePath(int? slot)
        {
            var name = Save.Class.fileName(slot).ToString();

            return FolderInfo.SaveRoot.GetFilePath(System.IO.Path.ChangeExtension(name, ".modded.json"));
        }

        void IOnCopySave.OnCopySave( IOnCopySave.EventData data )
        {
            var from = GetModdedSavePath(data.SlotFrom);
            var to = GetModdedSavePath(data.SlotTo);
            if (!System.IO.File.Exists(from))
            {
                return;
            }
            System.IO.File.Copy(from, to, true);
        }

        void IOnDeleteSave.OnDeleteSave( int? slot )
        {
            var to = GetModdedSavePath(slot);
            if (!System.IO.File.Exists(to))
            {
                return;
            }
            System.IO.File.Delete(to);
        }

        void IOnAfterLoadingSave.OnAfterLoadingSave( User data )
        {
            var path = GetModdedSavePath(null);
            if (System.IO.File.Exists(path))
            {
                currentSave = JObject.Parse(System.IO.File.ReadAllText(path));
            }
            else
            {
                currentSave = [];
            }
            EventSystem.BroadcastEvent<IOnAfterLoadingModdedSave, Func<string, JObject?>>(name =>
            {
                return currentSave[name] as JObject;
            });
        }

        void IOnAfterSavingSave.OnAfterSavingSave()
        {
            var path = GetModdedSavePath(null);
            currentSave ??= [];
            EventSystem.BroadcastEvent<IOnBeforeSavingModdedSave, Action<string, JObject?>>((name, data) =>
            {
                currentSave[name] = data ?? [];
            });
            System.IO.File.WriteAllText(path, currentSave.ToString());
        }
    }
}
