using Hashlink.Proxy;
using ModCore.Events;
using ModCore.Events.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Hashlink.Marshaling
{
    public unsafe class HashlinkObjHandle
    {
        private static readonly ReaderWriterLockSlim gcLock = new();
        private static readonly ConcurrentDictionary<nint, WeakReference<HandleInternal>> weakHandles = [];
        private static readonly ConcurrentDictionary<nint, HandleInternal> usedHandles = [];

        static HashlinkObjHandle()
        {
            EventSystem.AddReceiver(new EventReceiver());
        }

        private class EventReceiver : IEventReceiver, IOnNativeEvent
        {
            private readonly Dictionary<nint, HandleInternal>[] aliveHandles = [[], []];

            public int Priority => 0;

            public void OnNativeEvent( IOnNativeEvent.Event ev )
            {
                if (ev.EventId == IOnNativeEvent.EventId.HL_EV_BEGORE_GC)
                {
                    gcLock.EnterWriteLock();

                    // Save all currently existing handles
                    var cur = aliveHandles[1];
                    cur.Clear();
                    foreach ((var ptr, var wh) in weakHandles)
                    {
                        if (wh.TryGetTarget(out var h))
                        {
                            cur.Add(ptr, h);
                        }
                    }
                    usedHandles.Clear();
                    aliveHandles[0].Clear();
                    aliveHandles[1] = aliveHandles[0];
                    aliveHandles[0] = cur;
                }
                else if (ev.EventId == IOnNativeEvent.EventId.HL_EV_AFTER_GC)
                {
                    gcLock.ExitWriteLock();
                }
                else if (ev.EventId == IOnNativeEvent.EventId.HL_EV_GC_CS_NO_MARKED)
                {
                    if (weakHandles.TryGetValue(ev.Data, out var h) && h.TryGetTarget(out var handle))
                    {
                        // Allow the GC to recycle this handle
                        aliveHandles[0].Remove(ev.Data);
                    }
                }
            }
        }

        private class HandleInternal
        {
            private readonly void* target;
            public readonly HashlinkObjHandle handle;
            public HandleInternal( void* ptr )
            {
                target = ptr;
                handle = new(this);
                if (!hl_ptr_is_alive(ptr))
                {
                    throw new InvalidProgramException();
                }
                hl_add_root_2(ptr);
            }
            ~HandleInternal()
            {
                hl_remove_root_2(target);
            }
        }
        private static HandleInternal? GetInternalHandle( void* ptr )
        {
            if (!HashlinkMarshal.IsAllocatedHashlinkObject(ptr))
            {
                return null;
            }
            if (!hl_ptr_is_alive(ptr))
            {
                throw new InvalidProgramException();
            }
            gcLock.EnterUpgradeableReadLock();
            try
            {
                if (weakHandles.TryGetValue((nint)ptr, out var wh) && wh.TryGetTarget(out var handle))
                {
                    usedHandles.TryAdd((nint)ptr, handle);
                    return handle;
                }
                gcLock.EnterWriteLock();
                try
                {
                    handle = new(ptr);
                    weakHandles[(nint)ptr] = new(handle, true);
                    usedHandles.TryAdd((nint)ptr, handle);
                    return handle;
                }
                finally
                {
                    gcLock.ExitWriteLock();
                }

            }
            finally
            {
                gcLock.ExitUpgradeableReadLock();
            }
        }

        public static HashlinkObjHandle? GetHandle( void* ptr )
        {
            return GetInternalHandle(ptr)?.handle;
        }
        public static void MarkUsed( void* ptr )
        {
            _ = GetInternalHandle(ptr);
        }

        private HashlinkObj? obj;
        private readonly HandleInternal interHandle;
        private HashlinkObjHandle( HandleInternal handle )
        {
            interHandle = handle;
        }
        public HashlinkObj? Target
        {
            get => obj;
            set
            {
                if (obj != null && obj.TypeKind != TypeKind.HFUN /*Too bad*/)
                {
                    Debugger.Break();
                    throw new InvalidOperationException();
                }
                obj = value;
            }
        }
    }
}
