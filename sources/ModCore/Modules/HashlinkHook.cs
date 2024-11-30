using ModCore.Modules.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    public class HashlinkHook : CoreModule<HashlinkHook> , IOnBeforeGameStartup
    {
        public override int Priority => ModulePriorities.HashlinkHook;

        private NativeHook nhook = null!;

        public void OnBeforeGameStartup()
        {
            Logger.Information("Initializing");
            nhook = NativeHook.Instance;

        }


    }
}
