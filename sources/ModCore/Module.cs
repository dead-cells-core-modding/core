using ModCore.Events;
using Serilog;

namespace ModCore
{
    /// <summary>
    /// Base class for all modules
    /// </summary>
    /// <typeparam name="TModule"></typeparam>
    public abstract class Module<TModule> : Module where TModule : Module<TModule>
    {
        /// <summary>
        /// Get the module's logger
        /// </summary>
        public static new ILogger Logger { get; } = Log.ForContext("SourceContext", typeof(TModule).Name);

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException">Module initialized multiple times</exception>
        public Module()
        {
            if (instance != null && instance != this)
            {
                throw new InvalidOperationException();
            }
            instance = (TModule)this;
        }
        private static TModule? instance;

        /// <summary>
        /// Get an instance of a module
        /// </summary>
        public static TModule Instance
        {
            get
            {
                instance ??= EventSystem.FindReceiver<TModule>();
                return instance ?? throw new NullReferenceException();
            }
        }
    } 
    /// <summary>
    /// Base class for all modules
    /// </summary>
    public abstract class Module : IEventReceiver
    {
        private static readonly List<Module> modules = [];

        /// <inheritdoc/>
        public virtual int Priority => 0;

        /// <summary>
        /// Get the module's logger
        /// </summary>
        public ILogger Logger
        {
            get; private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Module()
        {
            Logger = Log.ForContext("SourceContext", GetType().Name);
            AddModule(this);
        }

        internal static void RemoveModule( Module module )
        {
            EventSystem.RemoveReceiver(module);
            modules.Remove(module);
        }

        internal static void AddModule( Module module )
        {
            EventSystem.AddReceiver(module);
            modules.Add(module);
        }
    }
}
