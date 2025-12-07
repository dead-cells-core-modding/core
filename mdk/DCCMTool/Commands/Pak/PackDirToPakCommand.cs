using CommandLine;
using CommandLine.Text;
using GameRes.Core.Pak;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands.Pak
{
    internal class PackDirToPakCommand : CommandBase<PackDirToPakCommand.Options>
    {
        public override async Task ExecuteAsync()
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
                if (splitIdx != -1)
                {
                    input = v[..splitIdx];
                    pakDir = pak.GetOrCreateDirectory(v[(splitIdx + 1)..]);
                }

                foreach(var file in Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileName(file);
                    var rpath = Path.GetRelativePath(input, Path.GetDirectoryName(file)!)
                        .Replace('\\', '/');
                    var dir = pakDir.GetDirectory(rpath, true);

                    Debug.Assert(dir.Name == Path.GetFileName(rpath));

                    var fentry = (PakFile.FileEntry?) dir.Entries.FirstOrDefault(x => x.Name == name);
                    if(fentry == null)
                    {
                        fentry = new()
                        {
                            Name = name
                        };
                        dir.Entries.Add(fentry);
                    }
                    fentry.Checksum = null;
                    fentry.Data = await File.ReadAllBytesAsync(file);
                }
            }

            using var stream = File.OpenWrite(Arguments.Output);
            pak.Write(new(stream));
        }

        [Verb("pack-pak", false, [
            "collapse-pak"
            ], HelpText = "Pack the contents of the folder into a pak file.")]
        public class Options
        {
            [Option('o', "output", HelpText = "The path to the output pak file.", Required = true)]
            public required string Output { get; set; }
            [Option('i', "input", HelpText = "The path to the input folder.", Required = true)]
            public required IEnumerable<string> Inputs { get; set; }
            [Option('s', "stamp", HelpText = "See https://n3rdl0rd.github.io/ModDocCE/files/pak/#stamps")]
            public string? Stamp { get; set; }

            [Usage]
            public static Example[] Examples => [
                new("Pack a single directory into a pak file", new Options(){
                    Inputs = [ "inputDir" ],
                    Output = "output.pak"
                }),
                new("Pack multiple directories into a pak file", new Options(){
                    Inputs = [ "inputDir1", "inputDir2" ],
                    Output = "output.pak"
                })
                ];
        }
    }
}
