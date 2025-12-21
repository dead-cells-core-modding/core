using GameRes.Core.Atlas.Colorswap;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameRes.Core.Atlas
{
    public unsafe static class ColorswapHelper
    {
        public static void DecodeImages(ColorswapPalette palette, params ReadOnlySpan<ImageResult> images)
        {
            foreach(var v in images)
            {
                if(v.Comp != ColorComponents.RedGreenBlueAlpha)
                {
                    throw new InvalidOperationException();
                }
                fixed(byte* data = v.Data)
                {
                    for(int i = v.Width * v.Height - 1; i >= 0; --i)
                    {
                        var col = Color32.Read(new(data + i * 4, 4), v.Comp);
                        if (col.A == 0)
                        {
                            continue;
                        }
                        var dcol = palette.DecodeColor(col);
                        dcol.Write(new(data + i * 4, 4), v.Comp);
                    }
                }
            }
        }
        public static void EncodeImages(ColorswapPalette palette, params ReadOnlySpan<ImageResult> images)
        {
            foreach(var v in images)
            {
                if(v.Comp != ColorComponents.RedGreenBlueAlpha)
                {
                    throw new InvalidOperationException();
                }

                fixed(byte* data = v.Data)
                {
                    for(int i = v.Width * v.Height - 1; i >= 0; --i)
                    {
                        var col = Color32.Read(new(data + i * 4, 4), v.Comp);
                        if (col.A == 0)
                        {
                            continue;
                        }
                        var ecol = palette.EncodeColor(col);
                        ecol.Write(new(data + i * 4, 4), v.Comp);
                    }
                }
            }
        }
        public static ColorswapPalette GeneratePalette(params ReadOnlySpan<ImageResult> images)
        {
            HashSet<Color32> colors = [];
            foreach(var v in images)
            {
                if(v.Comp != ColorComponents.RedGreenBlueAlpha)
                {
                    throw new InvalidOperationException();
                }
                fixed(byte* data = v.Data)
                {
                    for(int i = v.Width * v.Height - 1; i >= 0; --i)
                    {
                        var col = Color32.Read(new(data + i * 4, 4), v.Comp);
                        if(col.A == 0)
                        {
                            continue;
                        }
                        colors.Add(col);
                    }
                }
            }
            return ColorswapPalette.FromColors(colors.ToArray());
        }
        public static ColorswapPalette EncodeImages(params ReadOnlySpan<ImageResult> images)
        {
            var palette = GeneratePalette(images);
            EncodeImages(palette, images);
            return palette;
        }
    }
}
