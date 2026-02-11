using BytecodeMapping;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands
{
    internal class HaxeDebugInfoCommand : 
        CommandBase<HaxeDebugInfoCommand.Settings>
    {
        public override int Execute()
        {
            var db = BytecodeMappingData.ReadFrom(File.ReadAllBytes(Arguments.DatabasePath));
            int fid = -1;
            if (Arguments.FunctionIndex != null)
            {
                fid = Arguments.FunctionIndex.Value;
            }
            else
            {
                ArgumentNullException.ThrowIfNull(Arguments.Path, nameof(Arguments.Path));
                var name = Path.GetFileName(Arguments.Path);
                foreach (var v in db.Functions)
                {
                    foreach (var j in v.Value.Instructions)
                    {
                        if (j.Line != Arguments.Line)
                        {
                            continue;
                        }
                        if (Path.GetFileName(j.Path) == name)
                        {
                            fid = v.Key;
                            break;
                        }
                    }
                }
                if (fid == -1)
                {
                    throw new InvalidOperationException();
                }
            }

            var fun = db.Functions[fid];

            BytecodeMappingData.FunctionData.Item bestFit = new();
            foreach (var v in fun.Instructions)
            {
                if (Arguments.Path != null)
                {
                    if (Path.GetFileName(v.Path) != Arguments.Path)
                    {
                        continue;
                    }
                }
                if (v.Line >= bestFit.Line &&
                    v.Line <= Arguments.Line)
                {
                    bestFit = v;
                    if (v.Line == Arguments.Line)
                    {
                        break;
                    }
                }
            }

             AnsiConsole.WriteLine($"{fun.Name}{{IL Index: {bestFit.ILIndex}}}");
            return 0;
        }


        public class Settings : CommandSettings
        {
            [CommandOption("-i|--function-index")]
            [Description("The function index.")]
            public int? FunctionIndex { get; set; }

            [CommandOption("-p|--path")]
            [Description("The path of source file.")]
            public string? Path { get; set; }

            [CommandOption("-l|--line", true)]
            [Description("The line of source.")]
            public int Line { get; set; }

            [CommandOption("-d|--database", true)]
            [Description("The path of database. (*.bcm.bin)")]
            public string DatabasePath { get; set; } = "";

          
        }
    }
}
