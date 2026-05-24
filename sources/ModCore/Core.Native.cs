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
            //AddPath();

            _ = NN.Current.LoadLibrary(FolderInfo.CurrentNativeRoot.GetFilePath("modcorenative"));


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
                if (!NN.Current.TryLoadLibrary(v, out _))
                {
                    NN.Current.LoadLibrary(FolderInfo.CurrentNativeRoot.GetFilePath(v + ".so"));
                }
                nativeMembers.LoadAndActivateModule(v);
            }

            phLibhl = NN.Current.LoadLibrary("libhl");

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
                    var result = (nint)member.RVA + lib;
                    return result;
                }
                return NativeLibrary.GetExport(phLibhl, name);
            };
        }
    }
}
