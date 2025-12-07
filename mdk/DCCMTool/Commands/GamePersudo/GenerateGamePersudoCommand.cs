using CommandLine;
using DCCMTool.Commands;
using HashlinkNET.Bytecode;
using HashlinkNET.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands.GamePersudo
{
    internal class GenerateGamePersudoCommand : CommandBase<GenerateGamePersudoCommand.Options>
    {
        public override void Execute()
        {
            using AssemblyDefinition output = AssemblyDefinition.CreateAssembly(new(Arguments.Name, new()),
                Arguments.Name, ModuleKind.Dll);
            var hlcode = HlCode.FromBytes(File.ReadAllBytes(Arguments.Input));
            HashlinkCompiler compiler = new(hlcode, output, new()
            {
                AllowParalle = true,
                GeneratePseudocode = true,
                GenerateBytecodeMapping = Arguments.GenerateBCM,
            });
            compiler.Compile();

            Directory.CreateDirectory(Arguments.Output);
            var outputPath = Path.Combine(Arguments.Output, Arguments.Name + ".dll");
            using var pdbFile = new FileStream(Path.ChangeExtension(outputPath, "pdb"),
                FileMode.Create, FileAccess.Write);
            output.Write(outputPath, new()
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdbFile
            });
            if(Arguments.GenerateBCM)
            {
                File.WriteAllBytes(Path.ChangeExtension(outputPath, "bcm.bin"),
                    compiler.BytecodeMappingData.Write());
            }
        }

        [Verb("generate-game-persudo",
            HelpText = "Generate the pseudo-code assembly for hlboot.dat")]
        public class Options
        {
            [Option('i', "input", HelpText = "The path to the hlboot.dat.", Required = true)]
            public required string Input { get; set; }
            [Option('o', "output", HelpText = "The path to the output directory.", Required = true)]
            public required string Output { get; set; }
            [Option('n', "name", HelpText = "The name of output assembly.")]
            public string Name { get; set; } = "GamePersudocode";
            [Option("generate-bcm", HelpText = "Generate the bcm.bin file for the resolve-line-to-il command.", Default = false)]
            public bool GenerateBCM { get; set; } = false;
        }
    }
}
