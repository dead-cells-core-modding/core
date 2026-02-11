
using Serilog.Core;
using Spectre.Console.Cli;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCCMTool.Commands.Steam
{
    internal class UploadMAPICommand : SteamCommandBase<UploadMAPICommand.Settings>
    {
        public const uint MAPI_PFID = 3633185550;

        public class Settings : CommandSettings
        {
            [CommandOption("-i|--inputDir", true)]
            public required string InputDir { get; set; }
        }

        public override async Task<int> ExecuteSteamAsync()
        {
            var updateHandle = SteamUGC.StartItemUpdate(new(588650), new(MAPI_PFID));

            SteamUGC.SetItemContent(updateHandle, Arguments.InputDir);

            var ver = File.ReadAllText(Path.Combine(Arguments.InputDir, "ModCoreVersion.txt")).Trim();
            await SteamUGC.SubmitItemUpdate(updateHandle, $"Upload to v{ver}").Wait<RemoteStorageUpdatePublishedFileResult_t>();

            return 0;
        }
    }
}
