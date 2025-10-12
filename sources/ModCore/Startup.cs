
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.VM;
using ModCore.Native;
using ModCore.Storage;
using Serilog;
using System.Diagnostics;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ModCore
{
    internal static unsafe class Startup
    {

        public static int StartGame()
        {
            ContextConfig.SetReadonly();

            LogInitializer.InitializeLog();

            if (!Core.IsSlaveMode)
            {
                Console.Title = "Dead Cells with Core Modding";
            }
            Core.Initialize();

            var logger = Log.Logger.ForContext(typeof(Startup));
            //Load hlboot.dat


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
                if (!Core.IsSlaveMode)
                {
                    Native.Native.Current.InitializeGame(codeData, out var ctx);

                    logger.Information("Starting game");

                    EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                        new(IOnNativeEvent.EventId.HL_EV_START_GAME, (nint)Unsafe.AsPointer(ref ctx)));
                }

                return 0;

            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Uncaught .NET Exception");
                Debugger.BreakForUserUnhandledException(ex);
                throw;
            }
        }
   
    }
}
