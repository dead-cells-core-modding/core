using ModCore.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    public abstract class Module<TModule> : Module where TModule : Module<TModule>
    {
        public new static ILogger Logger { get; } = Log.ForContext("SourceContext", typeof(TModule).Name);
        internal Module()
        {
            if (instance != null && instance != this)
            {
                throw new InvalidOperationException();
            }
            instance = (TModule) this;
        }
        private static TModule? instance;
        public static TModule Instance
        {
            get
            {
                instance ??= EventSystem.FindReceiver<TModule>();
                return instance ?? throw new NullReferenceException();
            }
        }
    }
    public abstract class Module : IEventReceiver
    {
        private static readonly List<Module> modules = [];

        public virtual int Priority => 0;

        public ILogger Logger { get; private set; }
        internal Module()
        {
            Logger = Log.ForContext("SourceContext", GetType().Name);
        }

        
        internal static void AddModule(Module module)
        {
            EventSystem.AddReceiver(module);
            modules.Add(module);
        }
    }
}
