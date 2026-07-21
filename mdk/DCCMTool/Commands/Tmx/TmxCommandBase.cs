using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DCCMTool.Commands.Tmx
{
    public class TmxCommandSettings : CommandSettings
    {
        [CommandOption("-x|--xml", true)]
        public required string XmlFolder { get; set; }
        [CommandOption("-b|--bin", true)]
        public required string BinFolder { get; set; }
    }
    internal abstract class TmxCommandBase<TSettings> : CommandBase<TSettings> where TSettings : TmxCommandSettings
    {
        
        public static ProcessStartInfo BuildTmx(string option, string bin, string xml)
        {
            var root = PathUtils.GetDCCMRoot() ?? throw new InvalidOperationException("Unable to find DCCM");
            var binRoot = Path.Combine(root, "core", "native");
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                binRoot = Path.Combine(binRoot, "win-x64");
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                binRoot = Path.Combine(binRoot, "linux-x64");
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            var hlExePath = Path.Combine(binRoot, "hl");

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                File.SetUnixFileMode(hlExePath, File.GetUnixFileMode(hlExePath) | UnixFileMode.UserExecute);
            }
            return new ProcessStartInfo()
            {
                FileName = hlExePath,
                ArgumentList = {
                    $"tmxtool.hl",
                    $"-{option}",
                    "-TmxXml",
                    $"{Path.GetFullPath(xml)}",
                    "-TmxBin",
                    $"{Path.GetFullPath(bin)}"
                }
            };
        }
    }
}
