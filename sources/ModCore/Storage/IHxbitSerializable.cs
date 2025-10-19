using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Storage
{
    /// <summary>
    /// Should not be used directly, use <see cref="IHxbitSerializable{TData}"/>
    /// </summary>
    public interface IHxbitSerializable
    {
    
    }
    /// <summary>
    /// All types that can be serialized by hxbit should implement this interface
    /// </summary>
    /// <typeparam name="TData">A type used to store data</typeparam>
    public interface IHxbitSerializable<TData> : IHxbitSerializable
    {
        /// <summary>
        /// Returns the data that should be serialized when serialized by hxbit
        /// </summary>
        /// <returns></returns>
        TData GetData();
        /// <summary>
        /// Used to set the state of the object when deserialized by hxbit
        /// </summary>
        /// <param name="data"></param>
        void SetData(TData data);
    }
}
