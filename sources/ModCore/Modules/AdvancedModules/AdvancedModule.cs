namespace ModCore.Modules.AdvancedModules
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class AdvancedModuleAttribute : Attribute
    {

    }
    public abstract class AdvancedModule<TModule> : Module<TModule> where TModule : AdvancedModule<TModule>
    {
        internal AdvancedModule()
        {
        }
    }
}
