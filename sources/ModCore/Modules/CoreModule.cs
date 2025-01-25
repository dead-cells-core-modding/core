namespace ModCore.Modules
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CoreModuleAttribute : Attribute
    {
        public SupportOS supportOS = (SupportOS)(-1);
        [Flags]
        public enum SupportOS
        {
            Windows = 1,
            Linux = 2,
        }
    }
    public abstract class CoreModule<TModule> : Module<TModule> where TModule : CoreModule<TModule>
    {

        internal CoreModule()
        {
        }
    }
}
