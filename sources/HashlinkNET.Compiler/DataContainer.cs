using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler
{
    internal class DataContainer : IDataContainer
    {
        private class ObjectData
        {
            public readonly List<object> dataList = [];
            public readonly ReaderWriterLockSlim rwLock = new();
            public readonly Dictionary<Type, object?> dataLookup = [];
        }

        private readonly ConcurrentDictionary<object, ObjectData> table = new(ReferenceEqualityComparer.Instance);

        public IDataContainer? Parent
        {
            get; set;
        }

        private ObjectData GetObjectData( object obj )
        {
            return table.GetOrAdd(obj, _ => new());
        }
        public bool TryGetData<TData>( object? obj, [NotNullWhen(true)] out TData? data ) where TData : class
        {
            if (obj is null)
            {
                data = default;
                return false;
            }
            var od = GetObjectData( obj );
            od.rwLock.EnterReadLock();
            if (od.dataLookup.TryGetValue(typeof(TData), out var result) 
                && result is not null)
            {
                od.rwLock.ExitReadLock();
                data = (TData) result;
                return true;
            }
            od.rwLock.ExitReadLock();
            if (Parent is not null)
            {
                if (Parent.TryGetData(obj, out data))
                {
                    //TryAddData(obj, data);
                    return true;
                }
            }
            od.rwLock.EnterReadLock();
            bool inReadLock = true;
            try
            {
                result = od.dataList.FirstOrDefault(x => x is TData);
                if (result == null)
                {
                    data = default;
                }
                od.rwLock.ExitReadLock();
                inReadLock = false;
                od.rwLock.EnterWriteLock();
                try
                {
                    od.dataLookup.TryAdd(typeof(TData), result);
                    data = (TData?)result;
                    return data != null;
                }
                finally
                {
                    od.rwLock.ExitWriteLock();
                }
            }
            finally
            {
                if (inReadLock)
                {
                    od.rwLock.ExitReadLock();
                }
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
            try
            {
                od.rwLock.EnterWriteLock();
                if (!od.dataLookup.TryAdd(typeof(TData), data))
                {
                    //Try to add duplicate data. If this exception is thrown, it means that there is a serious bug in the compiler.
                    throw new InvalidOperationException("Try to add duplicate data. If this exception is thrown, it means that there is a serious bug in the compiler.");
                }
                od.dataList.Add(data);
                return data;
            }
            finally
            {
                od.rwLock.ExitWriteLock();
            }
        }

        public TData TryAddData<TData>( object obj, TData data ) where TData : class
        {
            var od = GetObjectData(obj);
            try
            {
                od.rwLock.EnterWriteLock();
                if (od.dataLookup.TryAdd(typeof(TData), data))
                {
                    od.dataList.Add(data);
                }
                
                return data;
            }
            finally
            {
                od.rwLock.ExitWriteLock();
            }
        }
    }
}
