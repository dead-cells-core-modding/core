using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DCCMTool.Commands.Pak
{
    internal class PakCommandSettings : CommandSettings
    {
        [CommandOption("-s|--stamp")]
        [Description("See https://n3rdl0rd.github.io/ModDocCE/files/pak/#stamps")]
        public string Stamp { get; set; } = string.Empty;
    }
}
