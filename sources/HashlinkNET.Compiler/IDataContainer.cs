using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler
{
    internal interface IDataContainer
    {
        IDataContainer? Parent
        {
            get;
            set;
        }

        TData AddData<TData>( object obj ) where TData : class, new()
        {
            return AddData( obj , new TData());
        }
        TData TryAddData<TData>( object obj, TData data ) where TData : class;
        TData AddData<TData>( object obj, TData data ) where TData : class;
        TData AddData<TData>( object obj, object obj2, TData data ) where TData : class
        {
            AddData(obj, data);
            AddData(obj2, data);
            return data;
        }
        void AddDataEach( object obj, object obj2)
        {
            AddData(obj, obj2);
            AddData(obj2, obj);
        }
        TData GetData<TData>( object obj ) where TData : class;
        bool TryGetData<TData>( object? obj, [NotNullWhen(true)] out TData? data ) where TData : class;
        TData GetGlobalData<TData>( ) where TData : class
        {
            return TryGetGlobalData<TData>(out var result) ? result : Parent!.GetGlobalData<TData>();
        }
        bool TryGetGlobalData<TData>( [NotNullWhen(true)] out TData? data ) where TData : class
        {
            return TryGetData(this, out data);
        }
        TData AddGlobalData<TData>( TData data ) where TData : class
        {
            return AddData(this, data);
        }
        TData AddGlobalData<TData>( ) where TData : class, new()
        {
            return AddData(this, new TData());
        }

        void Clear();
    }
}
