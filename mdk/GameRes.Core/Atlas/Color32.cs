using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameRes.Core.Atlas
{
    public readonly record struct Color32
    {
        public Color32(int r, int g, int b, int a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public int R { get; }
        public int G { get; }
        public int B { get; }
        public int A { get; }

        public static Color32 FromArgb(int a, int r, int g, int b) => new(r, g, b, a);
        public static Color32 Read(ReadOnlySpan<byte> data, ColorComponents color)
        {
            return color switch
            {
                ColorComponents.RedGreenBlue => new Color32(data[0], data[1], data[2], 255),
                ColorComponents.RedGreenBlueAlpha => new Color32(data[0], data[1], data[2], data[3]),
                ColorComponents.GreyAlpha => new Color32(data[0], data[0], data[0], data[1]),
                ColorComponents.Grey => new Color32(data[0], data[0], data[0], 255),
                _ => throw new ArgumentOutOfRangeException(nameof(color), "Unsupported color components"),
            };
        }
    }
}
