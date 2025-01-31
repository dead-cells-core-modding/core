using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.VM
{
    [Event(false)]
    public interface IOnResolveNativeLib
    {
        EventResult<nint> OnResolveNativeLib(string name);
    }
}
