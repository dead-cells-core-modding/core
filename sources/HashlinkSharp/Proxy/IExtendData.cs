using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy
{
    internal interface IExtendData
    {
        public T GetOrCreateData<T>(Func<HashlinkObj, object> factory) where T : class;
        public T GetData<T>() where T : class, IExtendDataItem => GetOrCreateData<T>(T.Create);
    }
    public interface IExtendDataItem
    {
        public static abstract object Create(HashlinkObj obj);
    }
}
