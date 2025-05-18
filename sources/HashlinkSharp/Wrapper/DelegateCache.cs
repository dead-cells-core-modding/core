using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Wrapper
{
    internal class DelegateCache<T>(T d) : IExtraDataItem where T : Delegate
    {
        public T Value
        {
            get;
        } = d;
        public static object Create( HashlinkObj obj )
        {
            return new DelegateCache<T>((T)((HashlinkClosure)obj).CreateDelegate(typeof(T)));
        }
    }
}
