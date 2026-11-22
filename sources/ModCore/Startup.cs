
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Storage;
using Serilog;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ModCore
{
    internal static unsafe class Startup
    {
        private const string Banner =
            """
            ██████╗  ██████╗ ██████╗ ███╗   ███╗
            ██╔══██╗██╔════╝██╔════╝████╗ ████║
            ██║  ██║██║     ██║     ██╔████╔██║
            ██║  ██║██║     ██║     ██║╚██╔╝██║
            ██████╔╝╚██████╗╚██████╗██║ ╚═╝ ██║
            ╚═════╝  ╚═════╝ ╚═════╝╚═╝     ╚═╝
            """;

        private static readonly Assembly CoreAssembly = typeof(Startup).Assembly;

        private static string Version =>
            CoreAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? CoreAssembly.GetName().Version?.ToString()
            ?? "Unknown";

        private static string Repository =>
            GetMetadata("RepositoryUrl") ?? "Unknown";

        private static string License =>
            GetMetadata("License") ?? "Unknown";


        private static string? GetMetadata(string key) =>
            CoreAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(x => x.Key == key)?.Value;

        public static int StartGame()
        {
            ContextConfig.SetReadonly();

            LogInitializer.InitializeLog();

            if (ContextConfig.Config.consoleOutput)
            {
                Console.WriteLine();
                Console.WriteLine(Banner);
                Console.WriteLine($"Dead Cells Core Modding v{Version}");
                Console.WriteLine($"Repository: {Repository}");
                Console.WriteLine($"License: {License}");
                Console.WriteLine("Not affiliated with Motion Twin.");
                Console.WriteLine();
            }

            Environment.SetEnvironmentVariable("SteamAPPId", "588650");

            Console.Title = "Dead Cells with Core Modding";
            Core.Initialize();

            var logger = Log.Logger.ForContext(typeof(Startup));
            //Load hlboot.dat

            Environment.CurrentDirectory = FolderInfo.GameRoot.FullPath;

            var hlbootPath = Environment.GetEnvironmentVariable("DCCM_HLBOOT_PATH");
            if (string.IsNullOrEmpty(hlbootPath))
            {
                hlbootPath = FolderInfo.GameRoot.GetFilePath("hlboot.dat");
            }

            ReadOnlySpan<byte> codeData;
            if (ContextConfig.Config.hlbcOverride == null)
            {
                logger.Information("Finding hlboot.dat");
                if (File.Exists(hlbootPath))
                {
                    codeData = File.ReadAllBytes(hlbootPath);
                }
                else
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var exeName = "deadcells_gl.exe";
                        logger.Information("Loading hlboot.dat from {name}", exeName);
                        codeData = Native.Native.Current.GetHlbootDataFromExe(FolderInfo.GameRoot.GetFilePath(exeName));
                        if (codeData.IsEmpty)
                        {
                            throw new FileNotFoundException(null, "hlboot.dat");
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException(null, "hlboot.dat");
                    }
                }
            }
            else
            {
                codeData = ContextConfig.Config.hlbcOverride.Value.Span;
            }
            try
            {
                logger.Information("Initializing game");
                Native.Native.Current.InitializeCore();
                Native.Native.Current.InitializeGame(codeData, out var ctx);

                logger.Information("Starting game");

                EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                    new(IOnNativeEvent.EventId.HL_EV_START_GAME, (nint)Unsafe.AsPointer(ref ctx)));
                return 0;

            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Uncaught .NET Exception");
                Debugger.BreakForUserUnhandledException(ex);
                Console.Error.WriteLine("[DCCM_FORCE_SHOW_ERROR_REPORT]");
                throw;
            }
        }

    }
}
