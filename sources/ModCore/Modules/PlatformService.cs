using HashlinkNET.Bytecode;
using ModCore.Events.Interfaces.VM;

namespace ModCore.Modules
{
    /// <summary>
    /// Provides access to information about the current game platform.
    /// </summary>
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    public class PlatformService :
        CoreModule<PlatformService>,
        IOnCodeLoading
    {

        /// <summary>
        /// Gets the identity of the current game platform.
        /// </summary>
        public GamePlatformIdentity CurrentPlatform {
            get; private set;
        } = GamePlatformIdentity.Unknown;
        void IOnCodeLoading.OnCodeLoading( ref ReadOnlySpan<byte> data )
        {
            var code = HlCode.FromBytes(data);
            if (code.Natives.Any(x => x.Lib == "hlsteam"))
            {
                //Steam Platform
                CurrentPlatform = GamePlatformIdentity.Steam;
            }
        }
    }
}
