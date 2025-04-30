using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces
{
    [Event(once: true)]
    internal interface IOnAdvancedModuleInitializing
    {
        public void OnAdvancedModuleInitializing();
    }
}
