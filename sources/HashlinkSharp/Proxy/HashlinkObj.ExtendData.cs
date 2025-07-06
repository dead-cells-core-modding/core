using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy
{
    public partial class HashlinkObj : IExtraData
    {
        private object? extendData;
        private readonly ReaderWriterLockSlim dataLock = new();

        private void ClearExtraData()
        {
            extendData = null;
        }

        T IExtraData.GetOrCreateData<T>( Func<HashlinkObj, object> factory )
        {
            
            if (this is T)
            {
                return (T)(object)this;
            }
            _RETRY:
            dataLock.EnterReadLock();
            if (extendData is T t)
            {
                dataLock.ExitReadLock();
                return t;
            }
            if (extendData == null)
            {
                dataLock.ExitReadLock();
                dataLock.EnterWriteLock();

                if (extendData != null)
                {
                    dataLock.ExitWriteLock();
                    goto _RETRY;
                }

                t = (T)factory(this);
                extendData = t;

                dataLock.ExitWriteLock();
                return t;
            }
            var list = extendData as ImmutableList<object>;
            
            if (list == null)
            {
                list = [extendData];
                extendData = list;
            }
            var lc = list.Count;
            for (int i = 0; i < lc; i++)
            {
                if (list[i] is T result)
                {
                    dataLock.ExitReadLock();
                    return result;
                }
            }
            dataLock.ExitReadLock();
            dataLock.EnterWriteLock();

            if (lc != list.Count)
            {
                dataLock.ExitWriteLock();
                goto _RETRY;
            }

            t = (T)factory(this);
            extendData = list.Add(t);

            dataLock.ExitWriteLock();

            return t;
        }
    }
}
