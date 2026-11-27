using dc.libs;
using Hashlink.Proxy;
using HaxeProxy.Runtime;
using ModCore.Events.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Modules.Internals.Fix
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    internal class DelayerFix : CoreModule<DelayerFix>,
        IOnAdvancedModuleInitializing
    {
        private readonly HaxeObjectFieldFastAccessor<dc.libs._Delayer.Task> accessor_t = new("t");


        void IOnAdvancedModuleInitializing.OnAdvancedModuleInitializing()
        {
            Hook_Delayer.addF += Hook_Delayer_addF;
            Hook_Delayer.addS += Hook_Delayer_addS;
            Hook_Delayer.addMs += Hook_Delayer_addMs;
        }

        private void InsertTask( Delayer self, dc.libs._Delayer.Task task )
        {
            var list = self.delays;
            list.push(task);

            var count = list.length;
            var items = list.array.AsSpan<HashlinkObjPtr>();

            int i = count - 1;
            var t = task.t;
            for (; i > 0; i--)
            {
                var prev = items[i - 1];
                if (!(t < accessor_t.GetValue<double>(prev)))
                {
                    break;
                }
                items[i] = prev;
                i--;
            }
            items[i] = HashlinkObjPtr.Get(task.HashlinkPointer);
        }

        private void Hook_Delayer_addMs( Hook_Delayer.orig_addMs orig, Delayer self, dc.String id, 
            HaxeProxy.Runtime.HlAction cb, double ms )
        {
            InsertTask(self, new(id, self.now + (ms / 1000) * self.fps, cb));
        }

        private void Hook_Delayer_addS( Hook_Delayer.orig_addS orig, Delayer self, dc.String id, 
            HaxeProxy.Runtime.HlAction cb, double sec )
        {
            InsertTask(self, new(id, self.now + sec * self.fps, cb));
        }

        private void Hook_Delayer_addF( Hook_Delayer.orig_addF orig, Delayer self, dc.String id, 
            HaxeProxy.Runtime.HlAction cb, double frames )
        {
            InsertTask(self, new(id, self.now + frames, cb));
        }
    }
}
