using CommandLine;
using GameRes.Core.Atlas;
using GameRes.Core.Atlas.Colorswap;
using StbImageSharp;
using StbImageWriteSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCCMTool.Commands.Atlas
{
    internal class DecodeColorswapCommand : CommandBase<DecodeColorswapCommand.Options>
    {
        [Verb("decode-colorswap", HelpText = "Decode colorswap to a more human-readable format.")]
        public class Options
        {
            [Option('i', "input", HelpText = "The path(s) to the colorswap file(s) to decode.", Required = true)]
            public required IEnumerable<string> Inputs { get; set; }
            [Option('p', "palette", HelpText = "The path to the colorswap palette file.", Required = true)]
            public required string PaletteFile { get; set; }
            [Option('o', "output", HelpText = "The directory to output the decoded files to.", Required = true)]
            public required string OutputDir { get; set; }
        }

        public override void Execute()
        {
            Directory.CreateDirectory(Arguments.OutputDir);

            var palette = new ColorswapPalette(ImageResult.FromMemory(File.ReadAllBytes(Arguments.PaletteFile), StbImageSharp.ColorComponents.RedGreenBlueAlpha));

            foreach(var v in Arguments.Inputs)
            {
                using var stream = File.OpenRead(v);
                var img = ImageResult.FromStream(stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
                ColorswapHelper.DecodeImages(palette, img);

                using var output = File.OpenWrite(Path.Combine(Arguments.OutputDir, Path.GetFileName(v)));

                new ImageWriter().WritePng(img.Data, img.Width, img.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha,
                    output);
            }
        }
    }
}
