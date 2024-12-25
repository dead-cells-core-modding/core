using Hashlink;
using ModCore.Hashlink.Transitions;
using ModCore.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hashlink.Hooks
{
    internal unsafe class HashlinkHookInst
    {
        internal Delegate? chain;

        private readonly HookTransition.HookTable* table;
        private readonly NativeHook.HookHandle hook;
        public HL_function* Target => table->func;
        public NativeHook.HookHandle Hook => hook;
        public void AddChain(Delegate entry)
        {
            chain = Delegate.Combine(entry);
            table->enabled = chain != null ? 1 : 0;
        }
        public void RemoveChain(Delegate entry)
        {
            chain = Delegate.Remove(chain, entry);
            table->enabled = chain != null ? 1 : 0;
        }

        internal HashlinkHookInst(HookTransition.HookTable* table, NativeHook.HookHandle hook)
        {
            this.table = table;
            this.hook = hook;
        }
    }
}
