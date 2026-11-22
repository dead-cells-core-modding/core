using dc;
using dc.haxe.io;
using dc.tool;
using ModCore.Events;
using ModCore.Events.Interfaces.Game.Save;
using ModCore.Storage;
using ModCore.Utilities;
using Newtonsoft.Json.Linq;

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

        public static string GetModdedSavePath( int? slot )
        {
            var name = Save.Class.fileName(slot).ToString();

            return System.IO.Path.ChangeExtension(name, ".modded.json");
        }

        void IOnCopySave.OnCopySave( IOnCopySave.EventData data )
        {
            var from = GetModdedSavePath(data.SlotFrom).AsHaxeString();
            var to = GetModdedSavePath(data.SlotTo).AsHaxeString();

            if (!dc.tool.File.Class.exists(from))
            {
                return;
            }
            dc.tool.File.Class.saveBytes(to, dc.tool.File.Class.getBytes(from));
        }

        void IOnDeleteSave.OnDeleteSave( int? slot )
        {
            var to = GetModdedSavePath(slot).AsHaxeString();
            if (!dc.tool.File.Class.exists(to))
            {
                return;
            }
            dc.tool.File.Class.delete(to);
        }

        void IOnAfterLoadingSave.OnAfterLoadingSave( User data )
        {
            var path = GetModdedSavePath(null).AsHaxeString();
            if (dc.tool.File.Class.exists(path))
            {
                var bytes = dc.tool.File.Class.getBytes(path);
                currentSave = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes.AsSpan()));
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
            EventSystem.BroadcastEvent<IOnBeforeSavingModdedSave, Action<string, JObject?>>(( name, data ) =>
            {
                currentSave[name] = data ?? [];
            });
            var data = System.Text.Encoding.UTF8.GetBytes(currentSave.ToString());
            var bytes = Bytes.Class.alloc(data.Length);
            data.CopyTo(bytes.AsSpan());
            dc.tool.File.Class.saveBytes(GetModdedSavePath(null).AsHaxeString(), bytes);
        }
    }
}
