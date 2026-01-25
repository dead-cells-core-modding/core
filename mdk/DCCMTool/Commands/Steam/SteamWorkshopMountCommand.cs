using CommandLine;
using Newtonsoft.Json.Linq;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DCCMTool.Commands.Steam
{
    internal class SteamWorkshopMountCommand : SteamCommandBase<SteamWorkshopMountCommand.Options>
    {
        public override async Task<int> ExecuteSteamAsync()
        {
            var gameRoot = Arguments.GameRoot;

            if(string.IsNullOrEmpty(gameRoot))
            {
                gameRoot = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH");
            }
            if(string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot))
            {
                Console.WriteLine("Game directory not found.");
                return -1;
            }

            var modRootPath = Path.Combine(gameRoot, "coremod", "mods");

            var modRoot = Path.Combine(modRootPath, Arguments.Name);

            if (Directory.Exists(modRoot) || File.Exists(modRoot))
            {
                Console.WriteLine($"The specified mod exists in the local mods folder. If you wish to proceed, please delete \"{modRoot}\".");
                return -1;
            }


            PublishedFileId_t? modId = null;

            {
                var handle = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(), EUserUGCList.k_EUserUGCList_Subscribed,
                  EUGCMatchingUGCType.k_EUGCMatchingUGCType_All, EUserUGCListSortOrder.k_EUserUGCListSortOrder_LastUpdatedDesc,
                  APPID, APPID, 1);

                SteamUGC.AddRequiredKeyValueTag(handle, "dccm_modname", Arguments.Name);

                var result = await SteamUGC.SendQueryUGCRequest(handle).Wait<SteamUGCQueryCompleted_t>();

                if (!SteamUGC.GetQueryUGCResult(handle, 0, out var pDetails))
                {
                    Console.WriteLine($"{Arguments.Name} was not found in the subscribed mods.");
                }
                else
                {
                    modId = pDetails.m_nPublishedFileId;
                }

                SteamUGC.ReleaseQueryUGCRequest(handle);
            }

            if(modId == null)
            {
                var handle = SteamUGC.CreateQueryAllUGCRequest(EUGCQuery.k_EUGCQuery_RankedByVote, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items,
                    APPID, APPID);


                SteamUGC.AddRequiredKeyValueTag(handle, "dccm_modname", Arguments.Name);

                await SteamUGC.SendQueryUGCRequest(handle).Wait<SteamUGCQueryCompleted_t>();

                if (SteamUGC.GetQueryUGCResult(handle, 0, out var pDetails))
                {
                    Console.WriteLine($"{Arguments.Name} was not found in the steam workshop.");
                }
                else
                {
                    modId = pDetails.m_nPublishedFileId;
                }

                SteamUGC.ReleaseQueryUGCRequest(handle);
            }
            if(modId == null)
            {
                Console.WriteLine("The specified mod cannot be found.");
                return -1;
            }
            var mod = modId.Value;

            _RE_TRY:

            if (!SteamUGC.GetItemInstallInfo(mod, out _, out var path, 1024, out _))
            {
                if (!Arguments.InstallModAuto)
                {
                    Console.WriteLine("The specified mod is not installed.");
                    return -2;
                }
                Console.WriteLine("Downloading mod...");
                SteamUGC.SubscribeItem(mod);
                SteamUGC.DownloadItem(mod, true);
                while (!((EItemState)SteamUGC.GetItemState(mod)).HasFlag(EItemState.k_EItemStateInstalled))
                {
                    await Task.Delay(100);
                }
                goto _RE_TRY;
            }

            Console.WriteLine("Mod Path: " + path);


            Directory.CreateSymbolicLink(modRoot, path);
            return 0;
        }

        [Verb("steam-workshop-mount", HelpText = "Mount Steam Workshop mods into the local mods folder.")]
        public class Options
        {
            [Option('n', "name", HelpText = "The name of the mod.", Required = true)]
            public required string Name { get; set; }
            [Option('a', "mod-auto-subscribe", Default = true, HelpText = "Automatically subscribe and install missing mods from Steam Workshop.")]
            public bool InstallModAuto { get; set; } = true;
            [Option("game", HelpText = "The path to the game root.")]
            public string? GameRoot { get; set; }
        }
    }
}
