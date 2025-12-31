using CommandLine;
using NonPublicNativeMembers;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCCMTool.Commands.Core
{
    internal class ScanNativePrivateMemberCommand : CommandBase<ScanNativePrivateMemberCommand.Options>
    {
        [Verb("scan-native-private-member", Hidden = true)]
        public class Options
        {
            [Option('b', "baseDir")]
            public string? BaseDir { get; set; }
            [Option('i', "inputs", Required = true, HelpText = "Input dlls to scan.")]
            public required IEnumerable<string> Inputs { get; set; }
            [Option('o', "output", Required = true, HelpText = "Output file path.")]
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
