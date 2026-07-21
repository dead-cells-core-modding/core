using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DCCMTool.Commands.Tmx
{
    internal class CollapseTmxCommand : TmxCommandBase<TmxCommandSettings>
    {
        public override async Task<int> ExecuteAsync()
        {
            var proc = Process.Start(BuildTmx("Collapse", Arguments.BinFolder, Arguments.XmlFolder));
            await proc!.WaitForExitAsync();
            return 0;
        }
    }
}
