using ModCore.Events.Interfaces;
using ModCore.Storage;

namespace ModCore.Modules
{
    /// <summary>
    /// 
    /// </summary>
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    public class StorageManager : CoreModule<StorageManager>, IOnCoreModuleInitializing
    {
        ///<inheritdoc/>
        public override int Priority => ModulePriorities.Storage;

        void IOnCoreModuleInitializing.OnCoreModuleInitializing()
        {
            Logger.Information("Game Root: {root}", FolderInfo.GameRoot.FullPath);
            Logger.Information("Mod Core Root: {root}", FolderInfo.CoreRoot.FullPath);


        }
    }
}
