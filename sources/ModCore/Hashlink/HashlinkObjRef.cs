using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hashlink
{
    internal unsafe class HashlinkObjRef
    {
        public nint hl_obj;
        public HashlinkObject cachedObj;

        private static readonly ReaderWriterLockSlim refsLock = new(LockRecursionPolicy.SupportsRecursion);
        private static readonly Dictionary<nint, WeakReference<HashlinkObjRef>> refs = [];

        public static HashlinkObjRef RegisterRef(HashlinkObject obj)
        {
            try
            {
                refsLock.EnterWriteLock();
                var @ref = new HashlinkObjRef((nint)obj.HashlinkObj, obj);
                if (!refs.TryAdd((nint)obj.HashlinkObj, new(@ref)))
                {
                    throw new InvalidOperationException();
                }
                return @ref;
            }
            finally
            {
                refsLock.ExitWriteLock();
            }
        }
        public static HashlinkObjRef GetRef(nint obj)
        {
            HashlinkObject? objRef = null;
            _RETRY:
            try
            {
                refsLock.EnterUpgradeableReadLock();
                if(refs.TryGetValue(obj, out var wref) && wref.TryGetTarget(out var result))
                {
                    GC.KeepAlive(objRef);
                    return result;
                }
                try
                {
                    refsLock.EnterWriteLock();
                    if (refs.TryGetValue(obj, out wref) && wref.TryGetTarget(out result))
                    {
                        GC.KeepAlive(objRef);
                        return result;
                    }
                    objRef = HashlinkObject.FromHashlinkInternal((void*)obj);
                    goto _RETRY;
                }
                finally
                {
                    refsLock.ExitWriteLock();
                }
            }
            finally
            {
                GC.KeepAlive(objRef);
                refsLock.ExitUpgradeableReadLock();
            }
            
        }

        private HashlinkObjRef(nint target, HashlinkObject obj)
        {
            hl_obj = target;
            cachedObj = obj;

            hl_add_root((void*)target);
        }
        ~HashlinkObjRef()
        {
            try
            {
                refsLock.EnterWriteLock();
                refs.Remove(hl_obj);
            }
            finally
            {
                refsLock.ExitWriteLock();
            }
            //hl_remove_root((void*)hl_obj);
        }
    }
}
