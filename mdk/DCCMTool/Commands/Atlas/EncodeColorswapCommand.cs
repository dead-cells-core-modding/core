using CommandLine;
using GameRes.Core.Atlas;
using StbImageSharp;
using StbImageWriteSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DCCMTool.Commands.Atlas
{
    internal class EncodeColorswapCommand : CommandBase<EncodeColorswapCommand.Options>
    {
        [Verb("encode-colorswap", HelpText = "Encode images into a colorswap palette and images.")]
        public class Options
        {
            [Option('i', "input", HelpText = "The path(s) to the colorswap file(s) to ecode.", Required = true)]
            public required IEnumerable<string> Inputs { get; set; }
            [Option('p', "palette", HelpText = "The name of the colorswap palette.")]
            public string? PaletteName { get; set; }
            [Option('o', "output", HelpText = "The directory to output the ecoded files to.", Required = true)]
            public required string OutputDir { get; set; }
        }
        public override int Execute()
        {
            var imgs = Arguments.Inputs.Select(x => ImageResult.FromMemory(File.ReadAllBytes(x), StbImageSharp.ColorComponents.RedGreenBlueAlpha)).ToArray();
            
            Directory.CreateDirectory(Arguments.OutputDir);

            var palette = ColorswapHelper.EncodeImages(imgs);

            if(string.IsNullOrEmpty(Arguments.PaletteName))
            {
                Arguments.PaletteName = Path.GetFileNameWithoutExtension(Arguments.Inputs.First()) + "_default_s";
            }

            using var paletteFile = File.OpenWrite(Path.Combine(Arguments.OutputDir, Arguments.PaletteName + ".png"));
            new ImageWriter().WritePng(palette.Palette.Data, palette.Palette.Width, palette.Palette.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, paletteFile);

            int index = 0;
            foreach(var v in Arguments.Inputs)
            {
                var img = imgs[index++];

                using var imgFile = File.OpenWrite(Path.Combine(Arguments.OutputDir, Path.GetFileName(v)));
                new ImageWriter().WritePng(img.Data, img.Width, img.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, imgFile);
            }
            return 0;
        }
    }
}
