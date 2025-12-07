using CommandLine;
using CommandLine.Text;
using GameRes.Core.Pak;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DCCMTool.Commands.Pak
{
    internal class PackFilesToPakCommand : CommandBase<PackFilesToPakCommand.Options>
    {
        public override void Execute()
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
        }

        [Verb("pack-pak-with-files", false, [], HelpText = "Pack files into a pak file.")]
        public class Options
        {
            [Option('o', "output", HelpText = "The path to the output pak file.", Required = true)]
            public required string Output { get; set; }
            [Option('i', "input", HelpText = "The path to the input files.", Required = true)]
            public required IEnumerable<string> Inputs { get; set; }
            [Option('s', "stamp", HelpText = "See https://n3rdl0rd.github.io/ModDocCE/files/pak/#stamps")]
            public string? Stamp { get; set; }

            [Usage]
            public static Example[] Examples => [
                new("Pack a single file into a pak file", new Options(){
                    Inputs = [ "inputFile=dir1/dir2/pathToFileInPak" ],
                    Output = "output.pak"
                }),
                new("Pack multiple directories into a pak file", new Options(){
                    Inputs = ["inputFile1=dir1/dir2/pathToFileInPak1", "inputFile2=dir1/dir2/pathToFileInPak2" ],
                    Output = "output.pak"
                })
                ];
        }
    }
}
