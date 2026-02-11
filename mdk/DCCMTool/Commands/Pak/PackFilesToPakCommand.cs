using GameRes.Core.Pak;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DCCMTool.Commands.Pak
{
    internal class PackFilesToPakCommand : CommandBase<PackFilesToPakCommand.Settings>
    {
        public override int Execute()
        {
            PakFile pak = new();
            if (!string.IsNullOrEmpty(Arguments.Stamp))
            {
                pak.Stamp = Encoding.ASCII.GetBytes(Arguments.Stamp).AsMemory()[..64];
            }
            foreach (var v in Arguments.Inputs)
            {
                PakFile.DirectoryEntry pakDir = pak.Root;
                string input = v;
                var splitIdx = v.IndexOf('=');
                string name;
                if (splitIdx != -1)
                {
                    input = v[..splitIdx];

                    var pakPath = v[(splitIdx + 1)..];
                    pakDir = pak.GetOrCreateDirectory(Path.GetDirectoryName(pakPath) ?? "");
                    name = Path.GetFileName(pakPath);
                }
                else
                {
                    name = Path.GetFileName(input);
                }

                var fentry = (PakFile.FileEntry?)pakDir.Entries.FirstOrDefault(x => x.Name == name);
                if (fentry == null)
                {
                    fentry = new()
                    {
                        Name = name
                    };
                    pakDir.Entries.Add(fentry);
                }
                fentry.Checksum = null;
                fentry.Data = File.ReadAllBytes(input);
            }

            using var stream = File.OpenWrite(Arguments.Output);
            pak.Write(new(stream));
            return 0;
        }

        public class Settings : PakCommandSettings
        {
            [CommandOption("-o|--output", true)]
            [Description("The path to the output pak file.")]
            public required string Output { get; set; }

            [CommandOption("-i|--inputs", true)]
            [Description("The path to the input files.")]
            public required IEnumerable<string> Inputs { get; set; }
        }
    }
}
