using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.VM
{
    /// <summary>
    /// An event is trigged when resolving a native library.
    /// </summary>
    [Event(false)]
    public interface IOnResolveNativeLib
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        EventResult<nint> OnResolveNativeLib(string name);
    }
}
