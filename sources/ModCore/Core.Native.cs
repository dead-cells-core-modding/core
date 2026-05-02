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

            _ = NativeLibrary.TryLoad(FolderInfo.CurrentNativeRoot.GetFilePath("modcorenative"), out _);


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
                if (!NativeLibrary.TryLoad(v, out _))
                {
                    NativeLibrary.Load(FolderInfo.CurrentNativeRoot.GetFilePath(v));
                }
                nativeMembers.LoadAndActivateModule(v);
            }

            phLibhl = NativeLibrary.Load("libhl");

            NN.GetLibhlSymbolFunc = name =>
            {
                var member = nativeMembers.Resolve(name);

                if (member != null)
                {
                    if (!loadedLibraries.TryGetValue(member.ModuleName, out var lib))
                    {
                        lib = NativeLibrary.Load(member.ModuleName);
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
