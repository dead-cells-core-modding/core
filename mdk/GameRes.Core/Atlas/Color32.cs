using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GameRes.Core.Atlas
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public readonly record struct Color32
    {
        public Color32(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        [field: FieldOffset(0)]
        public byte R { get; }
        [field: FieldOffset(1)]
        public byte G { get; }
        [field: FieldOffset(2)]
        public byte B { get; }
        [field: FieldOffset(3)]
        public byte A { get; }

        public static Color32 FromArgb(byte a, byte r, byte g, byte b) => new(r, g, b, a);
        public static Color32 Read(ReadOnlySpan<byte> data, ColorComponents color)
        {
            var result = color switch
            {
                ColorComponents.RedGreenBlue => new Color32(data[0], data[1], data[2], 255),
                ColorComponents.RedGreenBlueAlpha => new Color32(data[0], data[1], data[2], data[3]),
                ColorComponents.GreyAlpha => new Color32(data[0], data[0], data[0], data[1]),
                ColorComponents.Grey => new Color32(data[0], data[0], data[0], 255),
                _ => throw new ArgumentOutOfRangeException(nameof(color), "Unsupported color components"),
            };

            return result;
        }
        public void Write(Span<byte> data, ColorComponents color)
        {

            switch (color)
            {
                case ColorComponents.RedGreenBlue:
                    data[0] = (byte)R;
                    data[1] = (byte)G;
                    data[2] = (byte)B;
                    break;
                case ColorComponents.RedGreenBlueAlpha:
                    data[0] = (byte)R;
                    data[1] = (byte)G;
                    data[2] = (byte)B;
                    data[3] = (byte)A;
                    break;
                case ColorComponents.GreyAlpha:
                    data[0] = (byte)((R + G + B) / 3);
                    data[1] = (byte)A;
                    break;
                case ColorComponents.Grey:
                    data[0] = (byte)((R + G + B) / 3);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(color), "Unsupported color components");
            }
        }
    }
}
