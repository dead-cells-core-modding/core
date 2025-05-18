using dc;
using dc.libs.data;
using Hashlink.Marshaling;
using Hashlink.Proxy.DynamicAccess;
using ModCore.Events.Interfaces.Game;
using ModCore.Storage;
using ModCore.Utitities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GT = dc.libs.data.GetText;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    public class GetText : CoreModule<GetText>
    {
        public override int Priority => ModulePriorities.Game;

        public string GetString( string str )
        {
            var s = str.AsHaxeString();
            return Lang.Class.t.get(s, null).ToString()!;
        }
    }
}
