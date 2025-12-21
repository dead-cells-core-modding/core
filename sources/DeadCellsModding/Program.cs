using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

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

        [LibraryImport("nethost")]
        private static partial int get_hostfxr_path( Span<char> buffer, in nint buffer_size, nint __param );
        [LibraryImport("hostfxr", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int hostfxr_initialize_for_dotnet_command_line(int argc, string[] argv, nint parameters, out nint hostContext );
        [LibraryImport("hostfxr")]
        private static partial int hostfxr_run_app( nint hostContext );

        private static void LoadHost(string shellPath)
        {
            var nethostPath = Path.Combine(Path.GetDirectoryName(shellPath)!, "..", "native", "win-x64", "nethost.dll");
            NativeLibrary.Load(nethostPath);

            Span<char> hostfxrPathBuf = stackalloc char[2048];

            get_hostfxr_path(hostfxrPathBuf, hostfxrPathBuf.Length, 0);

            var hostfxrPath = new string(hostfxrPathBuf);

            NativeLibrary.Load(hostfxrPath);

            hostfxr_initialize_for_dotnet_command_line(1, [shellPath], 0, out var ctx);
            _ = hostfxr_run_app(ctx);
        }
        private static void StartGame()
        {
            var gameRoot = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH");
            if (string.IsNullOrEmpty(gameRoot))
            {
                gameRoot = Path.GetDirectoryName(Environment.ProcessPath!)!;
            }

            var steamid = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath!)!, "steam_appid.txt");
            if (!File.Exists(steamid))
            {
                try
                {
                    File.WriteAllText(steamid, "588650");
                }
                catch (Exception)
                {
                }
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

            Directory.SetCurrentDirectory(gameRoot!);

            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                var core = Assembly.LoadFrom(modcore);
                var startup = core.GetType("DCCMShell.Shell");
                startup!.GetMethod("StartFromShell")!.CreateDelegate<Action<nint, int>>()(0, 0);
            }
            else
            {
                LoadHost(modcore);
            }
        }
        private static void Main( string[] args )
        {
            StartGame();
        }
    }
}
