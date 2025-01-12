using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces
{
    public interface IOnHashlinkVMReady : ICallOnceEvent<IOnHashlinkVMReady>
    {
        public void OnHashlinkVMReady();
    }
}
