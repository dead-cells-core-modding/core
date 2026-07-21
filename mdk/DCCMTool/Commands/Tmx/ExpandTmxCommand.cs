using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DCCMTool.Commands.Tmx
{
    internal class ExpandTmxCommand : TmxCommandBase<TmxCommandSettings>
    {
        public override async Task<int> ExecuteAsync()
        {
            var pinfo = BuildTmx("Expand", Arguments.BinFolder, Arguments.XmlFolder);
            var proc = Process.Start(pinfo);
            await proc!.WaitForExitAsync();
            return 0;
        }
    }
}
