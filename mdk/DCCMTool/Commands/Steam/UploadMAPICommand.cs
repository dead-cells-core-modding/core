using CommandLine;
using Serilog.Core;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCCMTool.Commands.Steam
{
    internal class UploadMAPICommand  : CommandBase<UploadMAPICommand.Options>
    {
        public const uint MAPI_PFID = 3633185550;
        [Verb("upload-mapi", Hidden = true)]
        public class Options
        {
            [Option('i', "inputDir", Required = true)]
            public required string InputDir { get; set; }
        }

        public override async Task ExecuteAsync()
        {
            Environment.SetEnvironmentVariable("SteamAPPId", "588650");
            var initResult = SteamAPI.InitEx(out var err);

            if (initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Console.Error.WriteLine("Failed to initialize Steam API: {0} ({1})", err, initResult);
                Environment.Exit(-1);
            }

            

            CallResultUtils.StartLoop();

            var result = (EItemState) SteamUGC.GetItemState(new(MAPI_PFID));

            var updateHandle = SteamUGC.StartItemUpdate(new(588650), new(MAPI_PFID));

            SteamUGC.SetItemContent(updateHandle, Arguments.InputDir);

            var ver = File.ReadAllText(Path.Combine(Arguments.InputDir, "ModCoreVersion.txt")).Trim();
            var uploadC = await SteamUGC.SubmitItemUpdate(updateHandle, $"Upload to v{ver}").Wait<RemoteStorageUpdatePublishedFileResult_t>();
           
            Console.WriteLine($"{result:x}");
        }
    }
}
