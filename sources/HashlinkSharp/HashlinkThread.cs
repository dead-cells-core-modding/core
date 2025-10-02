using Hashlink.Events.Interfaces;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink
{
    public unsafe class HashlinkThread
    {
        [ThreadStatic]
        private static HashlinkThread? current;

        private class EventListener(WeakReference<HashlinkThread> thread) : IEventReceiver,
            IOnNativeEvent
        {
            public int Priority => 0;

            public void OnNativeEvent( IOnNativeEvent.Event ev )
            {
                if (!thread.TryGetTarget(out var t) ||
                    !t.Thread.IsAlive)
                {
                    EventSystem.RemoveReceiver(this);
                    return;
                }
                if (ev.EventId == IOnNativeEvent.EventId.HL_EV_GC_BEFORE_MARK)
                {
                    Native.Current.FixThreadCurrentStackFrame(t.NativeInfo);
                }
            }
        }

        public Thread Thread
        {
            get;
        } = Thread.CurrentThread;
        public HL_thread_info* NativeInfo
        {
            get;
        } = hl_get_thread();

        private HashlinkThread()
        {
            EventSystem.AddReceiver(new EventListener(new(this)));
        }

        public static void RegisterThread(nint stacktop = 0)
        {
            if (current != null)
            {
                throw new InvalidOperationException();
            }

            if (hl_get_thread() == null)
            {
                if (stacktop == 0)
                {
                    stacktop = (nint)Unsafe.AsPointer(ref stacktop);
                }

                hl_register_thread((void*)stacktop);
                hl_blocking(1);
            }

            current = new();
            current.NativeInfo->stack_cur = Unsafe.AsPointer(ref stacktop);

            EventSystem.BroadcastEvent<IOnRegisterHashlinkThread>();
        }
        public static void EnsureThreadRegistered()
        {
            if (current == null)
            {
                RegisterThread();
            }
        }
    
    }
}
