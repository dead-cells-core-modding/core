
using NonPublicNativeMembers;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCCMTool.Commands.Core
{
    internal class ScanNativePrivateMemberCommand : CommandBase<ScanNativePrivateMemberCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("-b|--baseDir")]
            public string? BaseDir { get; set; }

            [CommandOption("-i|--inputs", true)]
            public required string[] Inputs { get; set; }

            [CommandOption("-o|--output", true)]
            public required string Output { get; set; }
        }

        public override int Execute()
        {

            var manager = NativeMembersManager.Create();

            if(string.IsNullOrEmpty(Arguments.BaseDir))
            {
                manager.Generate([.. Arguments.Inputs]);
            }
            else
            {
                manager.Generate([.. Arguments.Inputs.Select(x => Path.Combine(Arguments.BaseDir, x))]);
            }
                

            File.WriteAllBytes(Path.Combine(Arguments.Output), manager.Save());
            return 0;

        }
    }
}
