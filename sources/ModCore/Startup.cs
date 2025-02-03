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
        [WillCallHL]
        private static int AfterCoreLoaded()
        {
            var logger = Log.Logger.ForContext(typeof(Startup));
            var useOpenGL = Environment.GetEnvironmentVariable("GAME_OPENGL")?.ToLower() == "true";
            //Load hlboot.dat
            var hlbootPath = Environment.GetEnvironmentVariable("DCCM_HLBOOT_PATH");
            if (string.IsNullOrEmpty(hlbootPath))
            {
                hlbootPath = FolderInfo.GameRoot.GetFilePath("hlboot.dat");
            }

            byte* hlboot = null;
            var hlbootSize = 0;
            logger.Information("Finding hlboot.dat");
            if (File.Exists(hlbootPath))
            {
                var mmf = MemoryMappedFile.CreateFromFile(hlbootPath);
                var view = mmf.CreateViewAccessor();
                view.SafeMemoryMappedViewHandle.AcquirePointer(ref hlboot);
                hlbootSize = (int)view.SafeMemoryMappedViewHandle.ByteLength;
            }
            else
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var exeName = useOpenGL ? "deadcells_gl.exe" : "deadcells.exe";
                    logger.Information("Loading hlboot.dat from {name}", exeName);
                    hlboot = (byte*)hlu_get_hl_bytecode_from_exe(FolderInfo.GameRoot.GetFilePath(exeName),
                        &hlbootSize);
                }
            }
            if (hlboot == null)
            {
                throw new FileNotFoundException(null, "hlboot.dat");
            }
            byte* err = null;
            logger.Information("Reading hl bytecode");

            hl_global_init();

            var codeData = new Span<byte>(hlboot, hlbootSize);

            EventSystem.BroadcastEvent<IOnCodeLoading, Span<byte>>(ref codeData);

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

        public static int StartGame()
        {
            Console.Title = "Dead Cells with Core Modding";
            Core.Initialize();

            return AfterCoreLoaded();
        }
    }
}
