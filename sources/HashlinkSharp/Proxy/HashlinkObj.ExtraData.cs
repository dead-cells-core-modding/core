using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Hashlink.Proxy
{
    public partial class HashlinkObj : IExtraData
    {
        private object? extraData;
        private Lock? dataWriteLock;

        private void ClearExtraData()
        {
            extraData = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T? TryGetExtraData<T>() where T : class
        {
            if (this is T)
            {
                return (T)(object)this;
            }

            var current = Volatile.Read(ref extraData);

            if (current is T t)
            {
                return t;
            }

            if (current is ImmutableList<object> l)
            {
                foreach (var v in l)
                {
                    if (v is T tv)
                    {
                        return tv;
                    }
                }
            }
            return null;
        }

        T IExtraData.GetOrCreateData<T>( Func<HashlinkObj, object> factory )
        {
            var result = TryGetExtraData<T>();

            if (result != null)
            {
                return result;
            }

            if (dataWriteLock is null)
            {
                Interlocked.CompareExchange(ref dataWriteLock, new(), null);

                Debug.Assert(dataWriteLock != null);
            }

            Interlocked.MemoryBarrier();

            lock (dataWriteLock)
            {
                result = TryGetExtraData<T>();
                if (result != null)
                {
                    return result;
                }

                result = (T)factory(this);

                if (extraData is null)
                {
                    extraData = result;
                    return result;
                }

                if (extraData is ImmutableList<object> ls)
                {
                    extraData = ls.Add(result);
                    return result;
                }

                extraData = ImmutableList.Create(extraData, result);
                return result;
            }
        }
    }
}
