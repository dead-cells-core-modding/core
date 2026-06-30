
using Serilog.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

            [CommandOption("-r|--release-info", true)]
            public required string ReleaseInfoPath { get; set; }
        }

        public override async Task<int> ExecuteSteamAsync()
        {
            var updateHandle = SteamUGC.StartItemUpdate(new(588650), new(MAPI_PFID));

            SteamUGC.SetItemContent(updateHandle, Arguments.InputDir);

            var ver = File.ReadAllText(Path.Combine(Arguments.InputDir, "ModCoreVersion.txt")).Trim();

            var cb = SteamUGC.SubmitItemUpdate(updateHandle, File.ReadAllText(Arguments.ReleaseInfoPath)).Wait<RemoteStorageUpdatePublishedFileResult_t>();

            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Uploading");
                    while (!task.IsFinished)
                    {
                        var progress = SteamUGC.GetItemUpdateProgress(updateHandle, out var bytesProcessed, out var bytesTotal);

                        if (progress == EItemUpdateStatus.k_EItemUpdateStatusInvalid)
                        {
                            break;
                        }
                        else if (progress == EItemUpdateStatus.k_EItemUpdateStatusPreparingConfig || 
                            progress == EItemUpdateStatus.k_EItemUpdateStatusPreparingContent)
                        {
                            task.Description = "Preparing...";
                        }
                        else if (progress == EItemUpdateStatus.k_EItemUpdateStatusUploadingContent)
                        {
                            task.Value = bytesTotal > 0 ? (double)bytesProcessed / bytesTotal * 100 : 0;
                            task.Description = $"Uploading... ({bytesProcessed}/{bytesTotal} bytes)";
                        }
                        else
                        {
                            task.Description = "Finalizing...";
                            task.Value = 100;
                        }
                        await Task.Delay(100);
                    }
                });

            var result = await cb;

            AnsiConsole.MarkupLine("[green]Done.[/]");


            return 0;
        }
    }
}
