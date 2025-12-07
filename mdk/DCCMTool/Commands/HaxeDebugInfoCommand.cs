using BytecodeMapping;
using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands
{
    internal class HaxeDebugInfoCommand : 
        CommandBase<HaxeDebugInfoCommand.Options>
    {
        public override void Execute()
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

            Console.WriteLine($"{fun.Name}{{IL Index: {bestFit.ILIndex}}}");
        }

        [Verb("resolve-line-to-il", false, [
            "resolve-line",
            ], HelpText = "Converts line numbers in error messages to IL sequence numbers in pseudo-code")]
        public class Options
        {
            [Option('i', "function-index", HelpText = "The function index.", Default = null)]
            public int? FunctionIndex { get; set; }
            [Option('p', "path", HelpText = "The path of source file.", Default = null)]
            public string? Path { get; set; }
            [Option('l', "line", HelpText = "The line of source.", Required = true)]
            public int Line { get; set; }
            [Option('d', "database", HelpText = "The path of database. (*.bcm.bin)", Required = true)]
            public string DatabasePath { get; set; } = "";

            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("Resolve from line and file name", new Options()
                    {
                        DatabasePath = "GameProxy.bcm.bin",
                        Line = 1072,
                        Path = "TimeKeeper.hx"
                    });
                }
            }
        }
    }
}
