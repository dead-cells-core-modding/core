using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.VM
{
    [Event(false)]
    public interface IOnResolveNativeFunction
    {
        public struct NativeFunctionInfo
        {
            public string libname;
            public string name;
        }
        EventResult<nint> OnResolveNativeFunction( NativeFunctionInfo info );
    }
}
