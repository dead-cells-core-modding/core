using Hashlink.Marshaling;
using Haxe.Marshaling;
using ModCore.Events.Interfaces.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    public class GetText : CoreModule<GetText>,
        IOnGameInit
    {
        public override int Priority => ModulePriorities.Game;
        private dynamic? gettext;
        void IOnGameInit.OnGameInit()
        {
            gettext = HashlinkMarshal.GetGlobal("Lang")!.AsHaxe().Dynamic.t;
        }

        public string GetString( string str )
        {
            return gettext?.get( str, null ).ToString() ?? str;
        }
    }
}
