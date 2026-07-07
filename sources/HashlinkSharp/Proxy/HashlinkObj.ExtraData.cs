using System.Collections.Immutable;

namespace Hashlink.Proxy
{
    public partial class HashlinkObj : IExtraData
    {
        private object? extraData;
        private readonly ReaderWriterLockSlim dataLock = new();

        private void ClearExtraData()
        {
            extraData = null;
        }

        T IExtraData.GetOrCreateData<T>( Func<HashlinkObj, object> factory )
        {

            if (this is T)
            {
                return (T)(object)this;
            }
            _RETRY:
            dataLock.EnterReadLock();
            if (extraData is T t)
            {
                dataLock.ExitReadLock();
                return t;
            }
            if (extraData == null)
            {
                dataLock.ExitReadLock();
                dataLock.EnterWriteLock();

                if (extraData != null)
                {
                    dataLock.ExitWriteLock();
                    goto _RETRY;
                }

                object result = factory(this);
                t = (T)result;
                extraData = t;

                dataLock.ExitWriteLock();
                return t;
            }
            var list = extraData as ImmutableList<object>;

            if (list == null)
            {
                list = [extraData];
                extraData = list;
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
            extraData = list.Add(t);

            dataLock.ExitWriteLock();

            return t;
        }
    }
}
