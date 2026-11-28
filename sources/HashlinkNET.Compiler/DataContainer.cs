using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace HashlinkNET.Compiler
{
    internal class DataContainer : IDataContainer
    {
        private sealed class ObjectData
        {
            public readonly List<object> dataList = [];
            public readonly ConcurrentDictionary<Type, object> dataLookup = [];
            public readonly object syncRoot = new();
        }

        // Sentinel stored in dataLookup to cache a "not found" result without
        // using null (ConcurrentDictionary values are kept non-null).
        private static readonly object MissingValue = new();

        private readonly ConcurrentDictionary<object, ObjectData> table = new(ReferenceEqualityComparer.Instance);

        public event Func<object, Type, object?>? TryResolve;

        public IDataContainer? Parent {
            get; set;
        }

        private ObjectData GetObjectData( object obj )
        {
            return table.GetOrAdd(obj, static _ => new ObjectData());
        }
        public bool TryGetData<TData>( object? obj, [NotNullWhen(true)] out TData? data ) where TData : class
        {
            if (obj is null)
            {
                data = default;
                return false;
            }

            var key = typeof(TData);

            // Fast path: exact-type cache hit without touching any lock.
            ObjectData? od = null;
            if (table.TryGetValue(obj, out var existing))
            {
                od = existing;
                if (od.dataLookup.TryGetValue(key, out var cached) &&
                    !ReferenceEquals(cached, MissingValue))
                {
                    data = (TData)cached;
                    return true;
                }
            }

            // Resolve from the parent (outer compiler) before allocating local
            // storage, so read-only misses do not create empty entries.
            if (Parent is not null && Parent.TryGetData(obj, out data))
            {
                return true;
            }

            od ??= GetObjectData(obj);
            lock (od.syncRoot)
            {
                if (od.dataLookup.TryGetValue(key, out var cached) &&
                    !ReferenceEquals(cached, MissingValue))
                {
                    data = (TData)cached;
                    return true;
                }

                object? result = null;
                foreach (var item in od.dataList)
                {
                    if (item is TData typed)
                    {
                        result = typed;
                        break;
                    }
                }

                if (result is null && TryResolve is not null)
                {
                    result = TryResolve(obj, key);
                }

                if (result is not null)
                {
                    od.dataLookup[key] = result;
                    data = (TData)result;
                    return true;
                }

                od.dataLookup.TryAdd(key, MissingValue);
                data = default;
                return false;
            }
        }
        public void Clear()
        {
            table.Clear();
        }
        public TData GetData<TData>( object obj ) where TData : class
        {
            return TryGetData<TData>(obj, out var result) ? result : throw new KeyNotFoundException();
        }
        public TData AddData<TData>( object obj, TData data ) where TData : class
        {
            var od = GetObjectData(obj);
            lock (od.syncRoot)
            {
                if (!od.dataLookup.TryAdd(typeof(TData), data))
                {
                    //Try to add duplicate data. If this exception is thrown, it means that there is a serious bug in the compiler.
                    throw new InvalidOperationException("Try to add duplicate data. If this exception is thrown, it means that there is a serious bug in the compiler.");
                }
                od.dataList.Add(data);
            }
            return data;
        }

        public TData TryAddData<TData>( object obj, TData data ) where TData : class
        {
            var od = GetObjectData(obj);
            lock (od.syncRoot)
            {
                if (od.dataLookup.TryAdd(typeof(TData), data))
                {
                    od.dataList.Add(data);
                }
            }
            return data;
        }
    }
}
