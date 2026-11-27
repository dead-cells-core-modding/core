using ModCore.Events.Interfaces;
using ModCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Modules.Internals
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    internal class SFXModule : CoreModule<SFXModule>,
        IOnCoreModuleInitializing
    {
        private const string ALSOFT_INI_CONTENT = """
[general]
channels = surround71
cf_level = 5
stereo-encoding = basic

""";
        void IOnCoreModuleInitializing.OnCoreModuleInitializing()
        {
            var alsoftini = Path.GetFullPath(FolderInfo.Cache.GetFilePath("alsoft_new.ini"));
            File.WriteAllText(alsoftini, ALSOFT_INI_CONTENT);

            Environment.SetEnvironmentVariable("ALSOFT_CONF", alsoftini);


            if (Core.Config.Value.UseOldSFX)
            {
                new LegacySFXModule().Init();
            }
        }
    }
}
