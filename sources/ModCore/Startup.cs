using Hashlink;
using ModCore.Storage;
using ModCore.Track;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    internal unsafe static class Startup
    {
        [WillCallHL]
        public static int StartGame()
        {
            Core.Initialize();

            var logger = Log.Logger.ForContext(typeof(Startup));

            //Load hlboot.dat
            var hlbootPath = FolderInfo.GameRoot.GetFilePath("hlboot.dat");
            byte* hlboot = null;
            int hlbootSize = 0;
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
                    logger.Information("Loading hlboot.dat from deadcells.exe");
                    hlboot = (byte*)Native.hlu_get_hl_bytecode_from_exe(FolderInfo.GameRoot.GetFilePath("deadcells.exe"), &hlbootSize);
                }
                else
                {
                    throw new FileNotFoundException(null, "hlboot.dat");
                }
            }
            byte* err = null;
            logger.Information("Reading hl bytecode");

            HashlinkNative.hl_global_init();

            var code = Native.hl_code_read(hlboot, hlbootSize, &err);
            logger.Information("Starting game");
            MixTrace.MarkEnteringHL();
            return Native.hlu_start_game(code);
        }
    }
}
