using CommandLine;
using GameRes.Core.Atlas;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCCMTool.Commands.Atlas
{
    internal class UpackAtlasCommand : CommandBase<UpackAtlasCommand.Options>
    {
        [Verb("unpack-atlas", HelpText = "Unpack an atlas file into its constituent images.")]
        public class Options
        {
            [Option('i', "input", HelpText = "The path to the atlas file to unpack.", Required = true)]
            public required string Input { get; set; }
            [Option('o', "output", HelpText = "The directory to output the unpacked files to.", Required = true)]
            public required string OutputDir { get; set; }
        }

        public override int Execute()
        {
            using var stream = File.OpenRead(Arguments.Input);
            var atlas = new AtlasData(stream, false);
            var root = Path.GetDirectoryName(Arguments.Input)!;

            atlas.UnpackAllTex(name => ImageResult.FromMemory(File.ReadAllBytes(Path.Combine(root, name))));

            Parallel.ForEach(atlas.Sprites, group =>
            {
                (var name, var frames) = group;

                int idx = 0;
                foreach(var f in frames)
                {
                    var output = Path.Combine(Arguments.OutputDir, name.Replace('.', '/') + $"-{idx++}.png");
                    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                    using var stream = File.OpenWrite(output);
                    f.UnpackedTex!.Extract().WriteTo(stream);
                }
            });

            return 0;
        }
    }
}
