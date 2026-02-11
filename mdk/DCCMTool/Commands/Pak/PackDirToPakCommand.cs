
using GameRes.Core.Pak;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands.Pak
{
    internal class PackDirToPakCommand : CommandBase<PackDirToPakCommand.Settings>
    {
        public override async Task<int> ExecuteAsync()
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
            return 0;
        }

        public class Settings : PakCommandSettings
        {
            [CommandOption("-o|--output", true)]
            [Description("The path to the output pak file.")]
            public required string Output { get; set; }
            [CommandOption("-i|--inputs", true)]
            [Description("The path to the input folder.")]
            public required IEnumerable<string> Inputs { get; set; }
        }
    }
}
