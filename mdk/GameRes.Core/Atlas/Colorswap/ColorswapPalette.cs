using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;

namespace GameRes.Core.Atlas.Colorswap
{
    public class ColorswapPalette
    {
        public record struct ColorEntry(int X, int Y);
        private readonly Color32[,] colorMapping = new Color32[256, 256];
        private readonly Dictionary<Color32, ColorEntry> mapping = [];

        private int nextX = 0;
        private int nextY = 0;

        private ImageResult? palette = null;

        public ImageResult Palette
        {
            get
            {
                CheckPalette();
                return palette;
            }
        }

        public ColorswapPalette(ImageResult palette)
        {
            this.palette = palette;

            if(palette.Comp != ColorComponents.RedGreenBlueAlpha)
            {
                throw new InvalidOperationException();
            }

            for (int y = 0; y < palette.Height; y++)
            {
                for (int x = 0; x < palette.Width; x++)
                {
                    var col = Color32.Read(palette.Data.AsSpan((y * palette.Width + x) * 4, 4), palette.SourceComp);
                    colorMapping[y, x] = col;
                    mapping[col] = new(x, y);
                }
            }
            nextY = palette.Height;
            nextX = 0;
        }

        private ColorswapPalette()
        {

        }

        [MemberNotNull(nameof(palette))]
        private void CheckPalette()
        {
            if(palette != null)
            {
                return;
            }

            palette = new()
            {
                Comp = ColorComponents.RedGreenBlueAlpha,
                SourceComp = ColorComponents.RedGreenBlueAlpha,
                Width = 256,
                Height = nextY + 1,
            };
            palette.Data = new byte[palette.Width * palette.Height * 4];

            for (int y = 0; y < palette.Height; y++)
            {
                for (int x = 0; x < palette.Width; x++)
                {
                    if (y == nextY && x >= nextX)
                    {
                        break;
                    }

                    colorMapping[y, x].Write(palette.Data.AsSpan((y * palette.Width + x) * 4, 4), ColorComponents.RedGreenBlueAlpha);
                }
            }
        }

        public static ColorswapPalette FromColors(params ReadOnlySpan<Color32> colors)
        {
            var result = new ColorswapPalette();
            result.InitializeFromColors(colors);
            return result;
        }

        private void InitializeFromColors(params ReadOnlySpan<Color32> colors)
        {
            nextX = 0;
            nextY = 0;
            foreach(var v in colors)
            {
                if (mapping.TryGetValue(v, out _))
                {
                    continue;
                }
                ColorEntry col = new(nextX++, nextY);
                if (nextX >= 256)
                {
                    nextX = 0;
                    nextY++;
                }
                mapping[v] = col;
                colorMapping[col.Y, col.X] = v;
            }
        }

        public Color32 EncodeColor(Color32 color)
        {
            if(!mapping.TryGetValue(color, out var entry))
            {
                throw new InvalidOperationException();
            }

            return new((byte)(((entry.X + 0.5f) / (float)Palette.Width) * 256), (byte)(((entry.Y + 0.5f) / (float) Palette.Height) * 256), 0, 255);
        }

        public Color32 DecodeColor(Color32 color)
        {
            int x = (int)Math.Round(color.R / 256f * (Palette.Width), MidpointRounding.ToZero);
            int y = (int)Math.Round(color.G / 256f * (Palette.Height), MidpointRounding.ToZero);

            return colorMapping[y, x];
        }
    }
}
