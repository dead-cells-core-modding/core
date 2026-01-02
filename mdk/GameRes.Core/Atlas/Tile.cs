using GameRes.Core.Utilities.RectpackSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameRes.Core.Atlas
{
    public class Tile
    {
        public required Image Image { get; set; }
        public PackingRectangle Rect { get; set; }

        public Image Extract()
        {
            var buffer = new Color32[Rect.Width * Rect.Height];
            for (int y = 0; y < Rect.Height; y++)
            {
                var srcRow = Image.Data.Span.Slice((int)((Rect.Y + y) * Image.Width + Rect.X), (int)Rect.Width);
                var dstRow = buffer.AsSpan().Slice((int)(y * Rect.Width), (int) Rect.Width);
                srcRow.CopyTo(dstRow);
            }
            return new()
            {
                Data = buffer,
                Height = (int)Rect.Height,
                Width = (int)Rect.Width,
            };
        }
    }
}
