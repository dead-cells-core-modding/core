using ModCore.Events.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Plugins
{
    public abstract class PluginBase : Module, IOnPluginInitializing
    {
        protected abstract void Initialize();
        void IOnPluginInitializing.OnPluginInitializing()
        {
            Logger.Information("Initializing");
            Initialize();
        }
    }
}
