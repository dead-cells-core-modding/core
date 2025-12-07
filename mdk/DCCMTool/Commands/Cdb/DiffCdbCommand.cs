using CommandLine;
using GameRes.Core.Cdb;
using GameRes.Core.Pak;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands.Cdb
{
    internal class DiffCdbCommand : CommandBase<DiffCdbCommand.Options>
    {
        public override void Execute()
        {
            var templateText = File.ReadAllText(Arguments.TemplateCDB);
            var srcText = File.ReadAllText(Arguments.CDBPath);
            if(templateText == srcText)
            {
                Console.WriteLine("Warning: Attempt to compare two same file.");
            }
            var templateCDB = CdbFile.ReadFrom(templateText);
            var srcCDB = CdbFile.ReadFrom(srcText);

            List<CdbLine> diff = [];
            foreach(var sheet in srcCDB.Sheets)
            {
                var tsheet = templateCDB.Sheets.First(x => x.Name == sheet.Name);
                
                var lines = sheet.Separators.SelectMany(x=>x.Lines).Where(x => x.Name != null).ToArray();
                var tlines = tsheet.Separators.SelectMany(x => x.Lines).Where(x => x.Name != null)
                    .ToImmutableDictionary(x => x.Name!);
                foreach(var l in lines)
                {
                    if(tlines.TryGetValue(l.Name!, out var tline))
                    {
                        if(tline.Value.ToString() == l.Value.ToString())
                        {
                            continue;
                        }
                    }

                    diff.Add(l);
                    if(Arguments.ShowDifference)
                    {
                        Console.WriteLine($"{sheet.Name}-{l.Separator.Name}-{l.Name}: {l.Value.Path}");
                    }
                }
            }
        
            if(!string.IsNullOrEmpty(Arguments.DiffPakPath))
            {
                var pak = new PakFile();
                var root = pak.GetOrCreateDirectory("data.cdb_");

                foreach(var v in diff)
                {
                    var dir = root.GetDirectory(v.Sheet.Name, true);
                    var val = (JObject) v.Value.DeepClone();
                    val["__separator_group_Name"] = v.Separator.Name ?? "";

                    var sepId = v.Sheet.Separators.IndexOf(v.Separator);
                    if (string.IsNullOrEmpty(v.Sheet.Separators[0].Name))
                    {
                        sepId--;
                    }

                    val["__separator_group_ID"] = sepId;
                    val["__original_Index"] = v.Separator.Lines.IndexOf(v);
                    var data = Encoding.UTF8.GetBytes(val.ToString());

                    dir.Entries.Add(new PakFile.FileEntry()
                    {
                        Name = v.Name! + ".json",
                        Data = data,
                    });
                }

                using var stream = File.OpenWrite(Arguments.DiffPakPath);
                pak.Write(new(stream));
            }
        }

        [Verb("diff-cdb", HelpText = "Compare the differences between two CDBs.")]
        public class Options
        {
            [Option('t',"template", Required = true, HelpText = "The path to the template cdb file.")]
            public required string TemplateCDB { get; set; }
            [Option('i', "cdb", Required = true, HelpText = "The path to the target cdb file.")]
            public required string CDBPath { get; set; }
            [Option("show-difference", HelpText = "Display the difference items on standard output.", Default = true)]
            public bool ShowDifference { get; set; }
            [Option('o', "output-pak", HelpText = "The path to the output differential pak file.You can then use merge-pak to combine it with other pak files.")]
            public string? DiffPakPath { get; set; }


        }
    }
}
