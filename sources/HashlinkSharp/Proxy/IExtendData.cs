using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy
{
    internal interface IExtendData
    {
        public T GetData<T>() where T : class, IExtendDataItem;
    }
    public interface IExtendDataItem
    {
        public static abstract object Create(HashlinkObj obj);
    }
}
