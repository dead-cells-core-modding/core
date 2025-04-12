using Hashlink.Proxy;
using ModCore.Events;
using ModCore.Events.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
            private nint[]? alivePtr;
            private readonly List<HandleInternal?>[] cachedHandles = [[], []];

            public int Priority => 0;

            public void OnNativeEvent( IOnNativeEvent.Event ev )
            {
                if (ev.EventId == IOnNativeEvent.EventId.HL_EV_BEGORE_GC)
                {
                    gcLock.EnterWriteLock();
                    
                }
                else if (ev.EventId == IOnNativeEvent.EventId.HL_EV_AFTER_GC)
                {
                    gcLock.ExitWriteLock();
                }
                else if (ev.EventId == IOnNativeEvent.EventId.HL_EV_GC_SEARCH_ROOT)
                {
                    // Save all currently existing handles
                    var cur = cachedHandles[1];

                    foreach ((var _, var wh) in weakHandles)
                    {
                        if (wh.TryGetTarget(out var h))
                        {
                            cur.Add(h);
                        }
                    }
                    usedHandles.Clear();
                    cachedHandles[0].Clear();
                    cachedHandles[1] = cachedHandles[0];
                    cachedHandles[0] = cur;

                    if (alivePtr == null || alivePtr.Length <= cur.Count)
                    {
                        alivePtr = GC.AllocateArray<nint>(cur.Capacity + 1, true);
                    }
                    for (int i = 0; i < cur.Count; i++)
                    {
                        var h = cur[i];
                        alivePtr[i] = h == null ? 0 : (nint)h.target;
                    }
                    var r = (IOnNativeEvent.Event_gc_roots*)ev.Data;
                    r->nroots = cur.Count;
                    r->roots = (void**) Unsafe.AsPointer(ref alivePtr[0]);
                }
                else if (ev.EventId == IOnNativeEvent.EventId.HL_EV_GC_CS_NO_MARKED)
                {
                    cachedHandles[0][(int)ev.Data] = null;
                }
            }
        }

        private class HandleInternal
        {
            internal readonly void* target;
            public readonly HashlinkObjHandle handle;
            public HandleInternal( void* ptr )
            {
                target = ptr;
                handle = new(this);
                if (!hl_ptr_is_alive(ptr))
                {
                    throw new InvalidProgramException();
                }
            }
            ~HandleInternal()
            {
                weakHandles.TryRemove((nint)target, out _);
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
                obj?.SetDestroyed();
                obj = value;
            }
        }
    }
}
