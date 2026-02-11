
using GameRes.Core.Atlas;
using GameRes.Core.Atlas.Colorswap;
using Spectre.Console.Cli;
using StbImageSharp;
using StbImageWriteSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DCCMTool.Commands.Atlas
{
    internal class DecodeColorswapCommand : CommandBase<DecodeColorswapCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("-i|--inputs", true)]
            [Description("The path(s) to the colorswap file(s) to decode.")]
            public required IEnumerable<string> Inputs { get; set; }

            [CommandOption("-p|--palette", true)]
            [Description("The path to the colorswap palette file.")]
            public required string PaletteFile { get; set; }

            [CommandOption("-o|--output <dir>")]
            [Description("The directory to output the decoded files to.")]
            public required string OutputDir { get; set; }
        }

        public override int Execute()
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

            return 0;
        }
    }
}
