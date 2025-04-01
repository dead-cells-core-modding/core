using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy
{
    public interface IExtendData
    {
        public void AddData( object data);
        public T GetData<T>();
    }
}
