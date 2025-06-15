using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Storage
{
    public interface IHxbitSerializable
    {
    
    }
    public interface IHxbitSerializable<TData> : IHxbitSerializable
    {
        TData GetData();
        void SetData(TData data);
    }
}
