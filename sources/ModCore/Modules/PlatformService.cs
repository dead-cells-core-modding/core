using HashlinkNET.Bytecode;
using ModCore.Events.Interfaces.VM;

namespace ModCore.Modules
{
    /// <summary>
    /// Provides access to information about the current game platform.
    /// </summary>
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    [Obsolete("Use GameInfo")]
    public class PlatformService :
        CoreModule<PlatformService>
    {

        /// <summary>
        /// Gets the identity of the current game platform.
        /// </summary>
        [Obsolete]
        public GamePlatformIdentity CurrentPlatform => (GamePlatformIdentity) GameInfo.Platform;
    }
}
