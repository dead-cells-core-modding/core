using Hashlink.Trace;
using ModCore.Events;
using ModCore.Events.Interfaces.VM;
using ModCore.Storage;
using Serilog;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace ModCore
{
    internal static unsafe class Startup
    {

        public static int StartGame()
        {
            LogInitializer.InitializeLog();

            Console.Title = "Dead Cells with Core Modding";
            Core.Initialize();

            var logger = Log.Logger.ForContext(typeof(Startup));
            //Load hlboot.dat
            var hlbootPath = Environment.GetEnvironmentVariable("DCCM_HLBOOT_PATH");
            if (string.IsNullOrEmpty(hlbootPath))
            {
                hlbootPath = FolderInfo.GameRoot.GetFilePath("hlboot.dat");
            }

            ReadOnlySpan<byte> codeData;

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
                    var hlbootSize = 0;
                    var hlboot = (byte*)hlu_get_hl_bytecode_from_exe(FolderInfo.GameRoot.GetFilePath(exeName),
                        &hlbootSize);
                    if (hlboot == null)
                    {
                        throw new FileNotFoundException(null, "hlboot.dat");
                    }
                    codeData = new(hlboot, hlbootSize);
                }
                else
                {
                    throw new FileNotFoundException(null, "hlboot.dat");
                }
            }
            byte* err = null;
            logger.Information("Reading hl bytecode");

            hl_global_init();

            EventSystem.BroadcastEvent<IOnCodeLoading, ReadOnlySpan<byte>>(ref codeData);

            void* code;
            fixed (byte* data = codeData)
            {
                code = hl_code_read(data, codeData.Length, &err);
            }
            if (err != null)
            {
                logger.Error("An error occurred while loading bytecode: {err}", Marshal.PtrToStringAnsi((nint)err));
                return -1;
            }
            try
            {
                logger.Information("Starting game");
                MixTrace.MarkEnteringHL();
                return hlu_start_game(code);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Uncaught .NET Exception");
                throw;
            }
        }
    }
}
