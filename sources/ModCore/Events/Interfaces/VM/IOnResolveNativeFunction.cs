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
    public interface IOnResolveNativeFunction
    {
        /// <summary>
        /// 
        /// </summary>
        public struct NativeFunctionInfo
        {
            /// <summary>
            /// The name of the native library
            /// </summary>
            public string libname;
            /// <summary>
            /// The name of the function
            /// </summary>
            public string name;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        EventResult<nint> OnResolveNativeFunction( NativeFunctionInfo info );
    }
}
