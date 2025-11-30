using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game.Save
{
    [Event()]
    internal interface IOnAfterLoadingModdedSave
    {
        void OnAfterLoadingModdedSave( Func<string, JObject?> getData );
    }
}
