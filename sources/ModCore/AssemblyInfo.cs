

using ModCore.Events.Interfaces.VM;
using ModCore.Storage;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TestRunner")]
[assembly: InternalsVisibleTo("ModCore.Game")]
[assembly: InternalsVisibleTo("DCCMShell")]

[assembly: TypeForwardedTo(typeof(IOnCodeLoading))]
[assembly: TypeForwardedTo(typeof(FolderInfo))]
[assembly: TypeForwardedTo(typeof(CacheFile))]
