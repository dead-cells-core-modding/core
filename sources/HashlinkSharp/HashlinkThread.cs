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

        public static HashlinkThread Current => current ?? throw new InvalidOperationException();

        public Thread Thread
        {
            get;
        } = Thread.CurrentThread;
        public HL_thread_info* NativeInfo
        {
            get;
        } = hl_get_thread();
        internal ref Native.NativeThreadLocal NativeData => ref native_tls_data[0];

        private readonly nint[] hl2cs_return_pointers_buffer = GC.AllocateArray<nint>(1024, true);
        private readonly Native.NativeThreadLocal[] native_tls_data = GC.AllocateArray<Native.NativeThreadLocal>(1, true);

        private HashlinkThread()
        {
            EventSystem.AddReceiver(new EventListener(new(this)));

            Native.Current.SetTlsValue(Native.Current.Data->tls_slot_index, (nint)Unsafe.AsPointer(ref NativeData));

            hl2cs_return_pointers_buffer[^1] = 1;
            NativeData.hl2cs_return_pointers = (nint)Unsafe.AsPointer(ref hl2cs_return_pointers_buffer[0]);
            
        }

        internal int ReturnPointerCount =>
            (int)(NativeData.hl2cs_return_pointers - (nint)Unsafe.AsPointer(ref hl2cs_return_pointers_buffer[0])) / sizeof(nint);
        internal ReadOnlySpan<nint> ReturnPointers => hl2cs_return_pointers_buffer.AsSpan(..ReturnPointerCount);

        internal void CleanupInvalidReturnPointers(nint ptr)
        {
            var bottom = (nint)Unsafe.AsPointer(ref hl2cs_return_pointers_buffer[0]);
            while (NativeData.hl2cs_return_pointers > bottom)
            {
                var val = *(nint*)bottom;
                if (val <= ptr || val == 0 || val == 1)
                {
                    NativeData.hl2cs_return_pointers = bottom;
                    return;
                }
                bottom += sizeof(nint);
            }
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
