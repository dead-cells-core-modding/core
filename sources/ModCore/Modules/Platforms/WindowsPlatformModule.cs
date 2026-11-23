using ModCore.Events.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Modules.Platforms
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    internal class WindowsPlatformModule : CoreModule<WindowsPlatformModule>,
        IOnCoreModuleInitializing
    {
        void IOnCoreModuleInitializing.OnCoreModuleInitializing()
        {
            
        }
    }
}
