using StbImageWriteSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameRes.Core.Atlas
{
    public unsafe class Image
    {
        public ReadOnlyMemory<Color32> Data { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public void WriteTo(Stream writer)
        {
            fixed (void* ptr = Data.Span)
            {
                new ImageWriter().WritePng(ptr, Width, Height, ColorComponents.RedGreenBlueAlpha, writer);
            }
        }
    }
}
