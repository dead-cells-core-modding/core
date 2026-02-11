using GameRes.Core.Atlas;
using Spectre.Console.Cli;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DCCMTool.Commands.Atlas
{
    internal class UpackAtlasCommand : CommandBase<UpackAtlasCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("-i|--input", true)]
            [Description("The path to the atlas file to unpack.")]
            public required string Input { get; set; }

            [CommandOption("-o|--output <dir>", true)]
            [Description("The directory to output the unpacked files to.")]
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
