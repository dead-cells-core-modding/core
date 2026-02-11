
using GameRes.Core.Pak;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands.Pak
{
    internal class UnpackPakCommand: CommandBase<UnpackPakCommand.Settings>
    {
        public override int Execute()
        {
            var output = new DirectoryInfo(Arguments.OutputDir);
            output.Create();

            var files = Arguments.Files?.ToArray() ?? [];


            foreach (var v in Arguments.PakPath)
            {
                using var stream = File.OpenRead(v);
                var pak = new PakFile(stream);


                static void ExtractToDirectory(PakFile.DirectoryEntry dir, DirectoryInfo output)
                {
                    foreach (var v in dir.Entries)
                    {
                        if (v is PakFile.DirectoryEntry d)
                        {
                            var doi = output.CreateSubdirectory(d.Name);
                            doi.Create();
                            ExtractToDirectory(d, doi);
                            continue;
                        }
                        else if (v is PakFile.FileEntry f)
                        {
                            File.WriteAllBytesAsync(
                                Path.Combine(output.FullName, v.Name),
                                f.Data.Data);
                        }
                    }
                }

                if(files.Length == 0)
                {
                    ExtractToDirectory(pak.Root, output);
                }
                else
                {
                    foreach(var f in files)
                    {
                        var entry = pak.GetEntry(f);
                        var dir = output;
                        var parent = Path.GetDirectoryName(f);
                        if(!string.IsNullOrEmpty(parent))
                        {
                            dir = dir.CreateSubdirectory(parent);
                        }
                        if(entry is PakFile.DirectoryEntry de)
                        {
                            ExtractToDirectory(de, dir.CreateSubdirectory(de.Name));
                        }
                        else if(entry is PakFile.FileEntry fs)
                        {
                            File.WriteAllBytesAsync(Path.Combine(dir.FullName, fs.Name), fs.Data.Data);
                        }
                    }
                } 
            }

            return 0;
        }
        public class Settings : PakCommandSettings
        {
            [CommandOption("-i|--inputs", true)]
            public required IEnumerable<string> PakPath { get; set; }

            [CommandOption("-f|--files", false)]
            [Description("The path to the file or directory to be unpacked.Leave blank to unpack all.")]
            public IEnumerable<string>? Files { get; set; }

            [CommandOption("-o|--output", true)]
            [Description("The path to the output directory.")]
            public required string OutputDir { get; set; }
        }
    }
}
