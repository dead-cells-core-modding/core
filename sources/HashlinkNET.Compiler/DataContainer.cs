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
            public readonly ReaderWriterLockSlim rwLock = new();
            public readonly Dictionary<Type, object?> data = [];
        }


        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<object, ObjectData>> table = [];

        public IDataContainer? Parent
        {
            get; set;
        }

        private ObjectData GetObjectData( object obj )
        {
            return table.GetOrAdd(obj.GetType(), _ => []).GetOrAdd(obj, _ => new());
        }
        public bool TryGetData<TData>( object? obj, [NotNullWhen(true)] out TData? data ) where TData : class
        {
            if (obj is null)
            {
                data = default;
                return false;
            }
            var od = GetObjectData( obj );
            if (od.data.TryGetValue(typeof(TData), out var result) && result is not null)
            {
                data = (TData) result;
                return true;
            }
            if (Parent is not null)
            {
                if (Parent.TryGetData(obj, out data))
                {
                    return true;
                }
            }
            od.rwLock.EnterUpgradeableReadLock();
            try
            {

                result = od.data.Values.FirstOrDefault(x => x is TData);
                if (result == null)
                {
                    data = default;
                }
                od.rwLock.EnterWriteLock();
                try
                {

                    od.data.TryAdd(typeof(TData), result);
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
                od.rwLock.ExitUpgradeableReadLock();
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
                if (!od.data.TryAdd(typeof(TData), data))
                {
                    //Try to add duplicate data. If this exception is thrown, it means that there is a serious bug in the compiler.
                    throw new InvalidOperationException("Try to add duplicate data. If this exception is thrown, it means that there is a serious bug in the compiler.");
                }
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
                od.data.TryAdd(typeof(TData), data);
                return data;
            }
            finally
            {
                od.rwLock.ExitWriteLock();
            }
        }
    }
}
