using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    public class FsPak : CoreModule<FsPak>,
        IOnBeforeGameInit
    {
        public override int Priority => ModulePriorities.Game;
        private dynamic fsPak = null!;
        private dynamic Hook_Reader_readHeader( HashlinkClosure orig, HashlinkObject self )
        {
            HashlinkMarshal.GetGlobal("hxd.fmt.pak.FileSystem")!.AsDynamic().PAK_STAMP_HASH = null;

            var data = ((HashlinkObject) orig.DynamicInvoke(self)!).AsDynamic();
            data.stampHash = null;
            return data;
        }
        private void Hook_FileSystem_loadPak( HashlinkClosure orig, HashlinkObject self, HashlinkObject path )
        {
            if (fsPak == null)
            {
                fsPak = self.AsDynamic();
            }
            Logger.Information("Loading pak from {path}", path.ToString());
            orig.DynamicInvoke(self, path);
        }

        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            HashlinkHooks.Instance.CreateHook("hxd.fmt.pak.Reader", "readHeader", Hook_Reader_readHeader).Enable();
            HashlinkHooks.Instance.CreateHook("hxd.fmt.pak.FileSystem", "loadPak", Hook_FileSystem_loadPak).Enable();
        }
    }
}
