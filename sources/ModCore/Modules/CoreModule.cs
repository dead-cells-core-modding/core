
using static ModCore.Modules.CoreModuleAttribute;

namespace ModCore.Modules
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class CoreModuleAttribute(
        CoreModuleKind kind,
        SupportOSKind supportOS = (SupportOSKind) (-1)
        ) : Attribute
    {
        public CoreModuleKind Kind
        {
            get;
        } = kind;
        public SupportOSKind SupportOS
        {
            get;
        } = supportOS;
        [Flags]
        public enum SupportOSKind
        {
            Windows = 1,
            Linux = 2,
        }
        public enum CoreModuleKind
        {
            Normal,
            Preload
        }

    }
    public abstract class CoreModule<TModule> : Module<TModule> where TModule : CoreModule<TModule>
    {

        internal CoreModule()
        {
        }
    }
}
