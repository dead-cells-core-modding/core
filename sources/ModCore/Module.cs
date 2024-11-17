using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    public class Module<TModule> : Module where TModule : Module<TModule>
    {
        public new static ILogger Logger { get; } = Log.ForContext<TModule>();
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
                instance ??= FindModule<TModule>();
                return instance ?? throw new NullReferenceException();
            }
        }
    }
    public class Module
    {
        private static readonly List<Module> modules = [];

        public virtual int Priority => 0;

        public ILogger Logger { get; private set; }
        internal Module()
        {
            Logger = Log.ForContext("SourceContext", GetType().FullName);
        }

        public static TModule? FindModule<TModule>() where TModule : Module
        {
            return modules.OfType<TModule>().FirstOrDefault();
        }
        public static IEnumerable<TModule> FindModules<TModule>() where TModule : Module
        {
            return modules.OfType<TModule>();
        }



        public static void BroadcastEvent<TEvent>()
        {
            foreach (var module in modules)
            {
                if (module is TEvent ev)
                {
                    ModuleEventCaller<TEvent>.Invoke(ev);
                }
            }
        }
        public static void BroadcastEvent<TEvent, TArg>(TArg arg)
        {
            foreach (var module in modules)
            {
                if (module is TEvent ev)
                {
                    ModuleEventCaller<TEvent>.Invoke(ev, ref arg);
                }
            }
        }

        internal static void AddModule(Module module)
        {
            modules.Add(module);
        }
    }
}
