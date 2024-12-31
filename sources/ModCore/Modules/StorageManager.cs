using ModCore.Events.Interfaces;
using ModCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ModCore.Modules
{
    [CoreModule]
    public class StorageManager : CoreModule<StorageManager>, IOnModCoreInjected
    {
        public override int Priority => ModulePriorities.Storage;

        void IOnModCoreInjected.OnModCoreInjected()
        {
            Logger.Information("Game Root: {root}", FolderInfo.GameRoot.FullPath);
            Logger.Information("Mod Core Root: {root}", FolderInfo.CoreRoot.FullPath);


        }
    }
}
