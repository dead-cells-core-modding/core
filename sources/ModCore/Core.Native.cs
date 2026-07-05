using ModCore.Storage;
using NonPublicNativeMembers;
using Serilog;
using System.Runtime.InteropServices;

using NN = ModCore.Native.Native;

namespace ModCore
{
    internal partial class Core
    {

        private readonly static NativeMembersManager nativeMembers = NativeMembersManager.Create();

        private static nint phLibhl;
        private static readonly Dictionary<string, nint> loadedLibraries = [];
        internal static void InitializeNative()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    NativeLibrary.Load(FolderInfo.CurrentNativeRoot.GetFilePath("libhl.dll"));
                }
                else
                {
                    NativeLibrary.Load(FolderInfo.CurrentNativeRoot.GetFilePath("libhl.so.1"));
                }
            }
            catch { }
            phLibhl = NN.Current.LoadLibrary("libhl");
            _ = NN.Current.LoadLibrary(FolderInfo.CurrentNativeRoot.GetFilePath("modcorenative"));
            _ = NN.Current.LoadLibrary(FolderInfo.CurrentNativeRoot.GetFilePath("libtcc"));


            foreach (var v in Directory.EnumerateFiles(FolderInfo.CurrentNativeRoot.FullPath, "*.json"))
            {
                var fn = Path.GetFileName(v);
                if (fn.StartsWith("nativemembers", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("Loading native member list from {path}", v);
                    nativeMembers.LoadFromFile(v);
                }
            }

            //Load hashlink libraries

            foreach (var v in ContextConfig.Config.hashlinkLibraries)
            {
                NN.Current.LoadLibrary(FolderInfo.CurrentNativeRoot.GetFilePath(v));
                nativeMembers.LoadAndActivateModule(v);
            }

           

            NN.GetLibhlSymbolFunc = name =>
            {
                var member = nativeMembers.Resolve(name);

                if (member != null)
                {
                    if (!loadedLibraries.TryGetValue(member.ModuleName, out var lib))
                    {
                        lib = NN.Current.LoadLibrary(member.ModuleName);
                        loadedLibraries[member.ModuleName] = lib;
                    }

                    // On Linux, dlopen (via NativeLibrary.Load) returns a link_map*
                    // whose first field l_addr is the actual load base address.
                    // On Windows, LoadLibrary returns HMODULE which is already the base.
                    nint baseAddr = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                        ? Marshal.ReadIntPtr(lib)
                        : lib;

                    return (nint)member.RVA + baseAddr;
                }
                return NativeLibrary.GetExport(phLibhl, name);
            };
        }
    }
}
