using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable IL2026
#pragma warning disable IL2075

namespace DeadCellsModding
{
    internal unsafe static partial class Program
    {

        private static string CombineModCore( string root )
        {
            return Path.Combine(root, "coremod", "core", "host", "DCCMShell.dll");
        }

        private static void LoadHostFail(uint result)
        {
            Console.WriteLine($"Failed to load coreclr. Error Code: 0x{result:x}");
            
            if (result == 0x80008096)
            {
                if (!Console.IsOutputRedirected && !Console.IsInputRedirected)
                {
                    // Open web
                    try
                    {
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "https://aka.ms/dotnet-core-applaunch?framework=Microsoft.WindowsDesktop.App&framework_version=11.0.0",
                            UseShellExecute = true,
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }

        [LibraryImport("nethost")]
        private static partial int get_hostfxr_path( Span<char> buffer, in nint buffer_size, nint __param );
        [LibraryImport("hostfxr", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int hostfxr_initialize_for_dotnet_command_line( int argc, string[] argv, nint parameters, out nint hostContext );
        [LibraryImport("hostfxr")]
        private static partial int hostfxr_run_app( nint hostContext );

        private static int LoadHost( string shellPath )
        {
            var nethostPath = Path.Combine(Path.GetDirectoryName(shellPath)!, "..", "native", "win-x64", "nethost.dll");
            NativeLibrary.Load(nethostPath);

            Span<char> hostfxrPathBuf = stackalloc char[2048];

            get_hostfxr_path(hostfxrPathBuf, hostfxrPathBuf.Length, 0);

            var hostfxrPath = new string(hostfxrPathBuf).Trim();

            if (!NativeLibrary.TryLoad(hostfxrPath, out _) || string.IsNullOrEmpty(hostfxrPath))
            {
                LoadHostFail(0x80008083);
                return -1;
            }

            var result = hostfxr_initialize_for_dotnet_command_line(1, [shellPath], 0, out var ctx);

            if (result < 0)
            {
                LoadHostFail((uint)result);
            }

            return hostfxr_run_app(ctx);
        }
        private static int StartGame( string[] args )
        {
            var gameRoot = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH");
            if (string.IsNullOrEmpty(gameRoot))
            {
                gameRoot = Path.GetDirectoryName(Environment.ProcessPath!)!;
            }

            string? modcore = null;
            gameRoot = Path.GetFullPath(gameRoot);
            while (!string.IsNullOrEmpty(gameRoot))
            {
                modcore = CombineModCore(gameRoot);
#if DEBUG
                Console.WriteLine("Try find ModCore in " + modcore);
#endif
                if (File.Exists(modcore))
                {
                    break;
                }
                gameRoot = Path.GetDirectoryName(gameRoot);
            }

            if (modcore == null || !File.Exists(modcore))
            {
                throw new FileNotFoundException(null, "DCCMShell.dll");
            }

            Environment.SetEnvironmentVariable("DEAD_CELLS_GAME_PATH", gameRoot);

            Directory.SetCurrentDirectory(gameRoot!);

            // Audio drivers such as Nahimic select the game's surround profile by
            // executable name. Keep the public launcher, but run the VM under the
            // original name so it receives the same device format and processing.
            if (OperatingSystem.IsWindows() &&
                !string.Equals(Path.GetFileName(Environment.ProcessPath), "deadcells.exe", StringComparison.OrdinalIgnoreCase))
            {
                var gameProcessPath = Path.Combine(Path.GetDirectoryName(modcore)!, "startup", "game", "deadcells.exe");
                if (!File.Exists(gameProcessPath))
                {
                    throw new FileNotFoundException("The game process launcher is missing. Reinstall the framework.", gameProcessPath);
                }
                var startInfo = new ProcessStartInfo(gameProcessPath)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = gameRoot!
                };
                foreach (var arg in args)
                {
                    startInfo.ArgumentList.Add(arg);
                }
                using var game = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the game process.");
                game.WaitForExit();
                return game.ExitCode;
            }

            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                var core = Assembly.LoadFrom(modcore);
                var startup = core.GetType("DCCMShell.Shell", true);

                startup!.GetMethod("StartFromShell")!.CreateDelegate<Action<nint, int>>()(0, 0);
                return 0;
            }
            else
            {
                return LoadHost(modcore);
            }
        }
        private static int Main( string[] args )
        {
            return StartGame(args);
        }
    }
}
