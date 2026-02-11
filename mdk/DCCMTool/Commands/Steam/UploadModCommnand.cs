
using DCCMTool.Commands.Cdb;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console.Cli;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DCCMTool.Commands.Steam
{
    internal class UploadModCommnand : SteamCommandBase<UploadModCommnand.Settings>
    {
        public override async Task<int> ExecuteSteamAsync()
        {
            var modroot = Path.GetFullPath(Arguments.ModPath);
            var modinfo = JObject.Parse(File.ReadAllText(Path.Combine(modroot, "modinfo.json")));

            var modname = modinfo["name"]!.ToString();
            var ver = modinfo["version"]!.ToString();

            Console.WriteLine($"Finding mod {modname} in workshop...");

            var handle = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(), EUserUGCList.k_EUserUGCList_Published,
                EUGCMatchingUGCType.k_EUGCMatchingUGCType_All, EUserUGCListSortOrder.k_EUserUGCListSortOrder_LastUpdatedDesc,
                APPID, APPID, 1);

            SteamUGC.AddRequiredKeyValueTag(handle, "dccm_modname", modname);

            var queryResult = await SteamUGC.SendQueryUGCRequest(handle).Wait<SteamUGCQueryCompleted_t>();

            var resultCount = queryResult.m_unNumResultsReturned;

            UGCUpdateHandle_t updateHandle = UGCUpdateHandle_t.Invalid;
            PublishedFileId_t publishId = PublishedFileId_t.Invalid;
            if (resultCount > 1)
            {
                SteamUGC.ReleaseQueryUGCRequest(handle);
                Console.Error.WriteLine("You appear to have uploaded multiple mods with the same name.");
                return -1;
            }
            else if (resultCount == 1)
            {
                if (!SteamUGC.GetQueryUGCResult(handle, 0, out var details))
                {
                    Console.Error.WriteLine("Failed to GetQueryUGCResult");
                    SteamUGC.ReleaseQueryUGCRequest(handle);
                    return -2;
                }
                SteamUGC.ReleaseQueryUGCRequest(handle);
                publishId = details.m_nPublishedFileId;
                updateHandle = SteamUGC.StartItemUpdate(APPID, details.m_nPublishedFileId);
            }
            else if (resultCount == 0)
            {
                SteamUGC.ReleaseQueryUGCRequest(handle);
                Console.WriteLine("Creating new item...");
                var r = await SteamUGC.CreateItem(APPID, EWorkshopFileType.k_EWorkshopFileTypeCommunity).Wait<CreateItemResult_t>();
                if (r.m_eResult != EResult.k_EResultOK)
                {
                    Console.WriteLine("Failed to create new item: " + r.m_eResult.ToString());
                    return -3;
                }
                updateHandle = SteamUGC.StartItemUpdate(APPID, r.m_nPublishedFileId);
                publishId = r.m_nPublishedFileId;

                SteamUGC.SetItemTitle(updateHandle, $"[DCCM] {modname}");
                SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
                SteamUGC.AddItemKeyValueTag(updateHandle, "dccm_modname", modname);
            }

            if (string.IsNullOrEmpty(Arguments.PreviewPath))
            {
                string[] previewExt = ["jpg", "png", "gif"];
                var previewPath = Path.Combine(Arguments.ModPath, "preview.");
                foreach (var v in previewExt)
                {
                    var rp = previewPath + v;
                    if (File.Exists(rp))
                    {
                        Console.WriteLine("Found preview: " + rp);
                        SteamUGC.SetItemPreview(updateHandle, Path.GetFullPath(rp));
                        break;
                    }
                }
            }
            else
            {
                SteamUGC.SetItemPreview(updateHandle, Path.GetFullPath(Arguments.PreviewPath));
            }

            SteamUGC.SetItemContent(updateHandle, modroot);

            if (string.IsNullOrEmpty(Arguments.UpdateText))
            {
                Arguments.UpdateText = "Update to v" + ver;
            }

            Console.WriteLine("Uploading...");

            var sresult = await SteamUGC.SubmitItemUpdate(updateHandle, Arguments.UpdateText).Wait<SubmitItemUpdateResult_t>();
            
            if(sresult.m_eResult != EResult.k_EResultOK)
            {
                Console.WriteLine("Unable to upload mod： " + sresult.m_eResult);
                Console.WriteLine("Please visit https://partner.steamgames.com/doc/api/ISteamUGC#SubmitItemUpdateResult_t for more information.");
                return -1;
            }

            Console.WriteLine("Mod Workshop Id: " + publishId.m_PublishedFileId);
            Console.WriteLine("You can access your mod here.: https://steamcommunity.com/sharedfiles/filedetails/?id=" + publishId.m_PublishedFileId);
            Console.WriteLine("Finished.");

            return 0;
        }


        public class Settings : CommandSettings
        {
            [CommandOption("-i|--input <path>", true)]
            [Description("The path for the mod directory.")]
            public required string ModPath { get; set; }

            [CommandOption("-t|--update-text <text>")]
            [Description("The update text for the mod upload.")]
            public string? UpdateText { get; set; }

            [CommandOption("-p|--preview <path>")]
            [Description("The path for the mod preview.")]
            public string? PreviewPath { get; set; }
        }
    }
}
