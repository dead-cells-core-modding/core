using ModCore.Events;
using ModCore.Events.Interfaces.VM;
using ModCore.Native;
using ModCore.Native.Events.Interfaces;
using ModCore.Storage;
using ModCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ModCore.Modules.Internals
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    internal class HLCModule : CoreModule<HLCModule>,
        IOnGetCompiledHLC
    {
        public override int Priority => 10001;

        private readonly CacheFile hlcCompiled = new("hlc_compiled.dat");

        public nint Libhlc
        {
            get; private set;
        }

        private void Tcc_OnError( string? obj )
        {
            if (!string.IsNullOrEmpty(obj))
            {
                if (obj.Contains(": warning:"))
                {
                    return;
                }
            }
            Logger.Error("tcc: {str}", obj);
        }

        private static void GenerateHLC( string dst, string hlboot )
        {
            var proc = Process.Start(new ProcessStartInfo()
            {
                FileName = "python",
                ArgumentList =
                {
                    "-m",
                    "crashlink",
                    "hlc",
                    "-o",
                    $"{dst}",
                    $"{hlboot}"
                },
                WorkingDirectory = FolderInfo.CoreRoot.GetFilePath("core/crashlink"),
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            });

            Debug.Assert(proc != null);

            proc.RedirectOutputToLogger(Logger);
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException("Failed to generate hlc!");
            }
        }

        EventResult<nint> IOnGetCompiledHLC.OnGetCompiledHLC( ReadOnlySpan<byte> data )
        {
            if (Libhlc != 0)
            {
                return Libhlc;
            }

            hlcCompiled.UpdateMetadata("hlbc", GameInfo.HlbootHash);

            if (!hlcCompiled.IsValid)
            {
                Logger.Information("Generating HLC...");

                var hlc_src = Path.Combine(hlcCompiled.CachePath + ".src");
                Directory.CreateDirectory(hlc_src);

                var hlboot = Path.Combine(hlc_src, "hlboot.dat");

                File.WriteAllBytes(hlboot, data);

                GenerateHLC(hlc_src, hlboot);

                Logger.Information("Compiling HLC...");

                var incRoot = Path.GetFullPath(Path.Combine(FolderInfo.CurrentNativeRoot.FullPath, "tinycc"));

                using var tcc = new TCCCompiler();

                tcc.OnError += Tcc_OnError;

                tcc.AddOptions(" -nostdinc -rdynamic -g1dwarf-5 ");

                tcc.SetOutputType(TCCCompiler.OutputType.DLL);
                tcc.AddIncludePath(incRoot, true);
                tcc.AddLibraryPath(incRoot);
                tcc.AddFile(Path.Combine(hlc_src, "build.c"));

                var result = tcc.Link(hlcCompiled.CachePath);
                if (result == -1)
                {
                    Logger.Error("Failed to compile HLC!");
                    throw new InvalidOperationException("Failed to compile HLC.");
                }

#if DEBUG
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && ContextConfig.Config.hlcPDB)
                {
                    Logger.Information("Generating pdb...");
                    var proc = Process.Start(new ProcessStartInfo()
                    {
                        FileName = FolderInfo.CurrentNativeRoot.GetFilePath("cv2pdb"),
                        ArgumentList =
                        {
                            hlcCompiled.CachePath
                        },
                        UseShellExecute = false
                    });

                    Debug.Assert(proc != null);

                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        throw new InvalidOperationException("Failed to generate pdb!");
                    }
                }
#endif

                hlcCompiled.UpdateCache();
            }
            Logger.Information("Loading HLC...");

            Libhlc = NativeLibrary.Load(hlcCompiled.CachePath);
            return Libhlc;
        }
    }
}
