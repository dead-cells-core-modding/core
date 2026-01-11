using Newtonsoft.Json.Linq;

namespace ModCore.Events.Interfaces.Game.Save
{
    [Event()]
    internal interface IOnAfterLoadingModdedSave
    {
        void OnAfterLoadingModdedSave( Func<string, JObject?> getData );
    }
}
