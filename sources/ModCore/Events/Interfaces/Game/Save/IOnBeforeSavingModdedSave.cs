using Newtonsoft.Json.Linq;

namespace ModCore.Events.Interfaces.Game.Save
{
    [Event()]
    internal interface IOnBeforeSavingModdedSave
    {
        void OnBeforeSavingModdedSave( Action<string, JObject> setData );
    }
}
