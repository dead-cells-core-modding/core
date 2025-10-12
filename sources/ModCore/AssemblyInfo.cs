

using ModCore.Events.Interfaces.VM;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TestRunner")]
[assembly: InternalsVisibleTo("ModCore.Game")]
[assembly: InternalsVisibleTo("DCCMShell")]

[assembly: TypeForwardedTo(typeof(IOnCodeLoading))]
