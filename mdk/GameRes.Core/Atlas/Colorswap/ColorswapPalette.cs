using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GameRes.Core.Atlas.Colorswap
{
    public class ColorswapPalette
    {
        public record struct ColorEntry(int X, int Y);
        private readonly Color32?[,] colorMapping = new Color32?[256, 256];
        private readonly Dictionary<Color32, ColorEntry> mapping = [];

        private int nextX = 0;
        private int nextY = 0;

        public ColorswapPalette(ImageResult palette)
        {
            var s = palette.SourceComp switch
            {
                ColorComponents.RedGreenBlueAlpha => 4,
                ColorComponents.RedGreenBlue => 3,
                _ => throw new InvalidOperationException()
            };

            for (int y = 0; y < palette.Height; y++)
            {
                for (int x = 0; x < palette.Width; x++)
                {
                    var col = Color32.Read(palette.Data.AsSpan((y * palette.Width + x) * s, s), palette.SourceComp);
                    colorMapping[y, x] = col;
                }
            }
            nextY = palette.Height;
            nextX = 0;
        }

        public ColorEntry GetOrAddColor(Color32 color)
        {
            if(mapping.TryGetValue(color, out var col))
            {
                return col;
            }
            col = new(nextX++, nextY);
            if(nextX >= 256)
            {
                nextX = 0;
                nextY++;
            }
            colorMapping[col.Y, col.X] = color;
            return col;
        }
    }
}
