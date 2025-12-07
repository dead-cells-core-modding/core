using CommandLine;
using GameRes.Core.Pak;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands.Pak
{
    internal class GenerateTemplatePakCommand : CommandBase<GenerateTemplatePakCommand.Options>
    {
        public const string TEMPLATE_MARK_NAME = ".dccm_tools_pak_diff_template";
        public override void Execute()
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
        }

        [Verb("generate-template-pak")]
        public class Options
        {
            [Option('i', "input", HelpText = "The path to the input pak file.", Required = true)]
            public required string Input { get; set; }
            [Option('o', "output", HelpText = "The path to the output pak file.", Required = true)]
            public required string Output { get; set; }
        }
    }
}
