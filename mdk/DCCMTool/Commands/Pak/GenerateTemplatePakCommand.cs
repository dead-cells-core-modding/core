
using GameRes.Core.Pak;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands.Pak
{
    internal class GenerateTemplatePakCommand : CommandBase<GenerateTemplatePakCommand.Settings>
    {
        public const string TEMPLATE_MARK_NAME = ".dccm_tools_pak_diff_template";
        public override int Execute()
        {
            using var inputFS = File.OpenRead(Arguments.Input);
            var input = new PakFile(inputFS);
            var output = new PakFile();

            static void ProcessDir(PakFile.DirectoryEntry src, PakFile.DirectoryEntry dst)
            {
                foreach(var v in src.Entries)
                {
                    if(v is PakFile.DirectoryEntry dir)
                    {
                        ProcessDir(dir, dst.GetDirectory(dir.Name, true));
                    }
                    else if(v is PakFile.FileEntry file)
                    {
                        dst.Entries.Add(new PakFile.FileEntry()
                        {
                            Name = file.Name,
                            Checksum = file.Checksum,
                            Data = SHA256.HashData(file.Data.Data.Span)
                        });
                    }
                }
            }

            ProcessDir(input.Root, output.Root);

            output.Root.Entries.Add(new PakFile.FileEntry()
            {
                Name = TEMPLATE_MARK_NAME,
                Checksum = -1,
                Data = "template"u8.ToArray()
            });

            using var outputFS = File.OpenWrite(Arguments.Output);
            output.Write(new(outputFS));
            return 0;
        }

        public class Settings : PakCommandSettings
        {
            [CommandOption("-i|--input", true)]
            [Description("The path to the input pak file.")]
            public required string Input { get; set; }
            [CommandOption("-o|--output", true)]
            [Description("The path to the output pak file.")]
            public required string Output { get; set; }
        }
    }
}
