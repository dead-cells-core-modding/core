using ModCore.Events;
using ModCore.Events.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Marshaling.ObjHandle
{
    internal unsafe static class HashlinkObjManager
    {
        private struct ObjHandle
        {
            public bool valid;
            public nint hlPtr;
            public GCHandle weakRef;
            public object? strongRef;

            public void Free()
            {
                Debug.Assert(valid);
                valid = false;
                *GetObjWrapperPtr((void*)hlPtr) = 0;
                hlPtr = 0;
            }
            public void Init( nint hlptr, object h )
            {
                valid = true;
                hlPtr = hlptr;
                if (!weakRef.IsAllocated)
                {
                    weakRef = GCHandle.Alloc(h, GCHandleType.WeakTrackResurrection);
                }
                else
                {
                    weakRef.Target = h;
                }
            }
        }

        private static readonly List<ObjHandle[]> handlePages = [ 
            [], new ObjHandle[1]
            ]; // 0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, ...
        private static ObjHandle[] currentHandlePage = handlePages[1];
        private static int currentHandlePageIndex = 1;
        private static int currentPageStartIndex = 0;
        private static int currentIndex = 0;

        static HashlinkObjManager()
        {
            EventSystem.AddReceiver(new NativeEventReceiver());
        }

        private class NativeEventReceiver : IEventReceiver, IOnNativeEvent
        {
            int IEventReceiver.Priority => -9999;

            private static nint[] rootsArray = [];
            private static int rootsCount;

            private static void SearchRoots( ref IOnNativeEvent.Event_gc_roots data )
            {
                int freeCount = 0;
                rootsCount = currentPageStartIndex + currentIndex;
                if (rootsArray.Length <= rootsCount)
                {
                    rootsArray = GC.AllocateArray<nint>(currentPageStartIndex + currentHandlePage.Length, true);
                }
                if (rootsCount == 0)
                {
                    data.nroots = 0;
                    return;
                }

                var hpidx = 0;
                var pageIndex = 0;
                var curPage = handlePages[0];
                for (int i = 0; i < rootsCount; i++)
                {
                    if (hpidx == curPage.Length)
                    {
                        curPage = handlePages[++pageIndex];
                        hpidx = 0;
                    }

                    ref var curHandle = ref curPage[hpidx++];

                    if (curHandle.valid && (curHandle.strongRef ??
                        curHandle.weakRef.Target) is HashlinkObjHandle)
                    {
                        rootsArray[i] = curHandle.hlPtr;
                        continue;
                    }

                    Debug.Assert(curHandle.weakRef.Target == null);

                    freeCount++;

                    //Find last alive obj
                    while (--rootsCount > i)
                    {
                        if (currentIndex == 0)
                        {
                            currentHandlePageIndex--;
                            currentHandlePage = handlePages[currentHandlePageIndex];
                            currentPageStartIndex = CalcPageStartIndex();
                            currentIndex = currentHandlePage.Length;
                        }
                        ref var h = ref currentHandlePage[--currentIndex];
                        if ((h.strongRef ?? h.weakRef.Target) is HashlinkObjHandle hh)
                        {
                            if (!hh.IsStateless)
                            {
                                h.strongRef = hh;
                            }
                            break;
                        }
                        else
                        {
                            h.Free();
                        }
                        freeCount++;
                    }

                    if (rootsCount == i)
                    {
                        //End
                        break;
                    }

                    //Swap
                    ref var old = ref currentHandlePage[currentIndex];

                    Debug.Assert(curHandle.weakRef.Target == null);
                    Debug.Assert(old.weakRef.Target != null);

                    curHandle.Free();
                    (old, curHandle) = (curHandle, old);

                    Debug.Assert(!currentHandlePage[currentIndex].valid);

                    var hobjHandle = (HashlinkObjHandle?) (curHandle.strongRef ?? 
                        curHandle.weakRef.Target);

                    Debug.Assert(hobjHandle != null);

                    hobjHandle.handleIndex = i;
                    rootsArray[i] = curHandle.hlPtr;
                }
                data.nroots = rootsCount;
                data.roots = (void**) Unsafe.AsPointer(ref rootsArray[0]);
            }

            private static void CleanStrongRef( )
            {
                var freeCount = 0;
                var genCount = 0;
                for (int i = 0; i < rootsCount; i++)
                {
                    if (rootsArray[i] == 0)
                    {
                        ref var sr = ref GetObjHandle(i).strongRef;
                        if (sr != null)
                        {
                            freeCount++;
                            genCount += GC.GetGeneration(sr);
                            sr = null;
                        }
                    }
                }
                if (freeCount * 5 > rootsCount)
                {
                    GC.Collect(genCount / freeCount, GCCollectionMode.Optimized, false);
                }
            }

            void IOnNativeEvent.OnNativeEvent( IOnNativeEvent.Event ev )
            {
                if (ev.EventId == IOnNativeEvent.EventId.HL_EV_BEGORE_GC)
                {
                    gcLock.EnterWriteLock();
                    GC.Collect(1, GCCollectionMode.Optimized, true);
                }
                else if (ev.EventId == IOnNativeEvent.EventId.HL_EV_AFTER_GC)
                {
                    CleanStrongRef();
                    gcLock.ExitWriteLock();
                    
                }
                else if (ev.EventId == IOnNativeEvent.EventId.HL_EV_GC_SEARCH_ROOT)
                {
                    var r = (IOnNativeEvent.Event_gc_roots*)ev.Data;
                    SearchRoots(ref Unsafe.AsRef<IOnNativeEvent.Event_gc_roots>(r));
                }
            }
        }

        private static int CalcPageStartIndex()
        {
            if (currentHandlePageIndex == 0 ||
                currentHandlePageIndex == 1)
            {
                return 0;
            }
            return (1 << (currentHandlePageIndex - 1)) - 1;
        }

        private static ref ObjHandle AllocObjHandle(out int index)
        {
            try
            {
                _RE_ALLOC:
                gcLock.EnterReadLock();
                var curStart = currentPageStartIndex;
                var curArray = currentHandlePage;
                var curIdx = Interlocked.Increment(ref currentIndex) - 1;

                if (curIdx >= curArray.Length)
                {
                    gcLock.ExitReadLock();
                    gcLock.EnterWriteLock();

                    if (currentHandlePage != curArray)
                    {
                        //Realloc
                        gcLock.ExitWriteLock();
                        goto _RE_ALLOC;
                    }
                    //Next page
                    currentHandlePageIndex++;
                    if (handlePages.Count == currentHandlePageIndex)
                    {
                        handlePages.Add(new ObjHandle[curArray.Length * 2]);
                    }
                    currentPageStartIndex = CalcPageStartIndex();
                    currentIndex = 0;
                    currentHandlePage = handlePages[currentHandlePageIndex];

                    gcLock.ExitWriteLock();
                    goto _RE_ALLOC;

                }

                index = curStart + curIdx;
                return ref curArray[curIdx];
            }
            finally
            {
                gcLock.ExitReadLock();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref ObjHandle GetObjHandle( int index )
        {
            var pageIndex = index == 0 ? 
                1 : (32 - BitOperations.LeadingZeroCount((uint)index + 1));
            var offset = index - ((1 << (pageIndex - 1)) - 1);
            var page = handlePages[pageIndex];

            return ref page[offset];
        }

        private static readonly ReaderWriterLockSlim gcLock = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint* GetObjWrapperPtr( void* ptr )
        {
            var size = hl_gc_get_memsize(ptr);
            if (size == -1)
            {
                return null;
            }
            return (nint*)((byte*)ptr + size - sizeof(void*));
        }
        public static HashlinkObjHandle? GetHandle( nint ptr )
        {
            var wp = GetObjWrapperPtr((void*)ptr);
            if (wp == null)
            {
                return null;
            }
            HashlinkObjHandle? handle;
            if (*wp != 0)
            {
                var gch = GCHandle.FromIntPtr(*wp);
                handle = gch.Target as HashlinkObjHandle;
                if (handle == null)
                {
                    *wp = 0;
                    return GetHandle(ptr);
                }

                Debug.Assert(handle.nativeHLPtr == ptr);
                

                ref var h = ref GetObjHandle(handle.handleIndex);

                Debug.Assert(h.valid);
                Debug.Assert(h.hlPtr == ptr);
                Debug.Assert(h.weakRef.Target == handle);

                if (handle.Target != null)
                {
                    Debug.Assert(*((nint*)ptr) == (nint)handle.Target.NativeType);
                }

                if (!handle.IsStateless)
                {
                    h.strongRef = handle;
                }

                return handle;
            }

            //Alloc New Handle
            {
                ref var h = ref AllocObjHandle(out var handleIdx);
                Debug.Assert(!h.valid);
                Debug.Assert(h.hlPtr == 0);
                h.valid = true;
                handle = new(ptr, handleIdx);
                h.Init(ptr, handle);
                if (!handle.IsStateless)
                {
                    h.strongRef = handle;
                }
                
                *wp = (nint)h.weakRef;
            }
            return handle;
        }
    }
}
