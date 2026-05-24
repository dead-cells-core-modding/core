using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DCCMTool.Commands
{
    internal class InstallCommand : CommandBase<InstallCommand.Settings>
    {
        public override async Task<int> ExecuteAsync()
        {
            Environment.SetEnvironmentVariable("DCCM_MDK_ROOT", Arguments.MDKRoot, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("DEAD_CELLS_GAME_PATH", Path.GetFullPath(
                Path.Combine(Arguments.MDKRoot, "..", "..", "..")
                ), EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("DCCM_MDK_BIN_ROOT", AppDomain.CurrentDomain.BaseDirectory, 
                EnvironmentVariableTarget.User);

            var p = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? "";
            if(!p.Contains("%DCCM_MDK_BIN_ROOT%"))
            {
                p += ";%DCCM_MDK_BIN_ROOT%";
                Environment.SetEnvironmentVariable("Path", p, EnvironmentVariableTarget.User);
            }

            Process.Start(new ProcessStartInfo("dotnet", $"nuget add source \"{Arguments.MDKRoot}/packages\"  --name DeadCoreModdingMDK"));

            return 0;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("--mdk", true)]
            public required string MDKRoot { get; set; }
        }
    }
}
