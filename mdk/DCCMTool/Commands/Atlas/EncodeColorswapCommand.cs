using GameRes.Core.Atlas;
using Spectre.Console.Cli;
using StbImageSharp;
using StbImageWriteSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace DCCMTool.Commands.Atlas
{
    internal class EncodeColorswapCommand : CommandBase<EncodeColorswapCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("-i|--inputs")]
            [Description("The path(s) to the colorswap file(s) to ecode.")]
            public required string[] Inputs { get; set; }

            [CommandOption("-p|--palette")]
            [Description("The name of the colorswap palette.")]
            public string? PaletteName { get; set; }

            [CommandOption("-o|--output <dir>", true)]
            [Description("The directory to output the ecoded files to.")]
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
