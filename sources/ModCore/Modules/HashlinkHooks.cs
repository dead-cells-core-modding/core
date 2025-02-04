using Hashlink;
using Hashlink.Brigde;
using Hashlink.Reflection.Members;
using ModCore.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class HashlinkHooks : CoreModule<HashlinkHooks>
    {
        private readonly Dictionary<nint, HashlinkHookManager> managers = [];

        public class HookHandle
        {

        }

        public HookHandle CreateHook(HashlinkFunction func, nint target = 0)
        {
            if(target == 0)
            {
                target = (nint) func.EntryPointer;
            }
            if(!managers.TryGetValue(target, out var manager))
            {
                manager = new(target, func);
                managers.Add(target, manager);
            }
            throw new NotImplementedException();
        }
    }
}
