using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCCMTool.Commands.Steam
{
    internal abstract class SteamCommandBase<TOption> : CommandBase<TOption>
    {
        public static readonly AppId_t APPID = new(588650);
        public sealed override async Task<int> ExecuteAsync()
        {
            Environment.SetEnvironmentVariable("SteamAPPId", "588650");
            var initResult = SteamAPI.InitEx(out var err);

            if (initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Console.Error.WriteLine("Failed to initialize Steam API: {0} ({1})", err, initResult);
                return -1;
            }

            CallResultUtils.StartLoop();
            
            try
            {
                return await ExecuteSteamAsync();
            }
            finally
            {
                CallResultUtils.StopLoop();
                SteamAPI.Shutdown();
            }
        }

        public abstract Task<int> ExecuteSteamAsync();
    }
}
