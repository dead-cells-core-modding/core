using CommandLine;
using HashlinkNET.Bytecode;
using HashlinkNET.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands.Core
{
    internal class GenerateGamePersudoCommand : CommandBase<GenerateGamePersudoCommand.Options>
    {
        private static void GenerateRefAssembly(AssemblyDefinition asm)
        {
            var m = asm.MainModule;

            asm.CustomAttributes.Clear();
            asm.CustomAttributes.Add(new(
                m.ImportReference(
                    typeof(ReferenceAssemblyAttribute).GetConstructors().First()
                )
                )
            {

            });

            static void CleanupType(TypeDefinition v)
            {
                foreach (var me in v.Methods.ToArray())
                {
                    if (!me.IsPublic)
                    {
                        v.Methods.Remove(me);
                    }
                    else if (me.Body != null)
                    {
                        me.Body = new(me);
                        var il = me.Body.GetILProcessor();
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Throw);
                    }
                }
                foreach (var f in v.Fields.ToArray())
                {
                    if (!f.IsPublic)
                    {
                        v.Fields.Remove(f);
                    }
                }
                foreach (var nt in v.NestedTypes.ToArray())
                {
                    if (!nt.IsPublic && !nt.IsNestedPublic)
                    {
                        v.NestedTypes.Remove(nt);
                    }
                    else
                    {
                        CleanupType(nt);
                    }
                }
            }

            var mscorlibRef = asm.MainModule.AssemblyReferences.First(x => x.Name == "mscorlib");
            var corelibRef = asm.MainModule.AssemblyReferences.FirstOrDefault(x => x.Name == "System.Private.CoreLib");
            if (corelibRef != null)
            {
                corelibRef.Culture = mscorlibRef.Culture;
                corelibRef.Version = mscorlibRef.Version;
                corelibRef.Attributes = mscorlibRef.Attributes;
                corelibRef.MetadataToken = mscorlibRef.MetadataToken;
                corelibRef.PublicKeyToken = mscorlibRef.PublicKeyToken;
                corelibRef.PublicKey = mscorlibRef.PublicKey;
                corelibRef.Hash = mscorlibRef.Hash;
                corelibRef.HashAlgorithm = mscorlibRef.HashAlgorithm;
                corelibRef.HasPublicKey = mscorlibRef.HasPublicKey;
                corelibRef.IsRetargetable = mscorlibRef.IsRetargetable;
                corelibRef.IsWindowsRuntime = mscorlibRef.IsWindowsRuntime;
                corelibRef.Name = mscorlibRef.Name;
            }
        }
        public override int Execute()
        {
            using AssemblyDefinition output = AssemblyDefinition.CreateAssembly(new(Arguments.Name, new()),
                Arguments.Name, ModuleKind.Dll);
            var hlcode = HlCode.FromBytes(File.ReadAllBytes(Arguments.Input));
            HashlinkCompiler compiler = new(hlcode, output, new()
            {
                AllowParalle = true,
                GeneratePseudocode = !Arguments.GenerateRefAssembly,
                GenerateBytecodeMapping = Arguments.GenerateBCM,
            });
            compiler.Compile();

            if(Arguments.GenerateRefAssembly)
            {
                GenerateRefAssembly(output);
            }

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
            return 0;
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
            [Option("generate-ref-assembly", HelpText = "Generate a reference assembly instead of a full one.", Default = false)]
            public bool GenerateRefAssembly { get; set; } = false;
        }
    }
}
