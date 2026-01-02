using GameRes.Core.Utilities.RectpackSharp;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GameRes.Core.Atlas
{
    public unsafe class AtlasData
    {
        public class TileData
        {
            public string? OriginalTileName { get; set; }
            public string AtlasTexName { get; set; } = "";
            public Tile? UnpackedTex { get; set; }
            public PackingRectangle RealRect { get; set; }
            public PackingRectangle TrimmedRect { get; set; }
        }

        public Dictionary<string, List<TileData>> Sprites { get; set;  } = [];

        public AtlasData()
        {
        }

        private void ReadFromBinary(BinaryReader data)
        {
            string ReadString()
            {
                int num = data.ReadByte();
                if(num == 255)
                {
                    num = data.ReadUInt16();
                }
                return new string(data.ReadChars(num));
            }

            Dictionary<string, Dictionary<int, TileData>> tiles = [];

            while(true)
            {
                var atlasName = ReadString();
                if(string.IsNullOrEmpty(atlasName))
                {
                    break;
                }

                while (true)
                {
                    var tileName = ReadString();
                    if(string.IsNullOrEmpty(tileName))
                    {
                        break;
                    }
                    int idx = data.ReadUInt16();
                    uint x = data.ReadUInt16();
                    uint y = data.ReadUInt16();
                    uint w = data.ReadUInt16();
                    uint h = data.ReadUInt16();
                    uint offx = data.ReadUInt16();
                    uint offy = data.ReadUInt16();
                    uint ow = data.ReadUInt16();
                    uint oh = data.ReadUInt16();

                    FixIndex(tileName, out var realName, ref idx);

                    if (!tiles.TryGetValue(realName, out var tileGroup))
                    {
                        tileGroup = [];
                        tiles[realName] = tileGroup;
                    }
                    tileGroup.Add(idx, new()
                    {
                        AtlasTexName = atlasName,
                        OriginalTileName = tileName,
                        TrimmedRect = new(x, y, w, h),
                        RealRect = new(offx, offy, ow, oh),
                    });
                }
            }
            foreach ((var name, var tileGroup) in tiles)
            {
                var list = new List<TileData>(tileGroup.Count);
                for (int i = 0; i < tileGroup.Count; ++i)
                {
                    list.Add(tileGroup[i]);
                }
                Sprites[name] = list;
            }
        }

        private static void FixIndex(string name, out string realName, ref int realIdx)
        {
            var idx = name.LastIndexOf('_');
            if(idx == -1)
            {
                realName = name;
                return;
            }
            var num = name[(idx + 1)..];
            if(!int.TryParse(num, out var parsedIdx))
            {
                realName = name;
                return;
            }
            Debug.Assert(realIdx == 0);
            realName = name[..idx];
            realIdx = parsedIdx;
        }

        private void ReadFromText(string text)
        {
            throw new NotImplementedException();
        }

        public void UnpackAllTex(Func<string, ImageResult> textureLoader)
        {
            var buffer_size = Sprites.Values.SelectMany(x => x).Sum(x => x.RealRect.Width * x.RealRect.Height);
            var buffer = new Color32[buffer_size];
            int bufferIdx = 0;

            Dictionary<string, ImageResult> cachedTex = [];

            foreach (var sprite in Sprites.Values)
            {
                foreach (var tile in sprite)
                {
                    if (tile.UnpackedTex != null)
                        continue;

                    if(!cachedTex.TryGetValue(tile.AtlasTexName, out var img))
                    {
                        img = textureLoader(tile.AtlasTexName);
                        cachedTex[tile.AtlasTexName] = img;
                    }
                    if(img.Comp != ColorComponents.RedGreenBlueAlpha)
                    {
                        throw new InvalidOperationException($"Unsupported image format: {img.Comp}");
                    }

                    var size = tile.RealRect.Width * tile.RealRect.Height;
                    var sp = buffer.AsMemory(bufferIdx, (int)size);
                    bufferIdx += (int)size;
                    var image = new Image()
                    {
                        Data = sp,
                        Width = (int)tile.RealRect.Width,
                        Height = (int)tile.RealRect.Height,
                    };
                    var t = new Tile()
                    {
                        Image = image,
                        Rect = new(0, 0, tile.RealRect.Width, tile.RealRect.Height)
                    };
                    
                    for (int y = 0; y < tile.TrimmedRect.Height; y++)
                    {
                        for (int x = 0; x < tile.TrimmedRect.Width; x++)
                        {
                            sp.Span[(int)((y + tile.RealRect.Y) * tile.RealRect.Width + x + tile.RealRect.X)] = 
                                Color32.Read(img.Data.AsSpan(((y + (int)tile.TrimmedRect.Y) * img.Width + x + (int)tile.TrimmedRect.X) * 4, 4), 
                                ColorComponents.RedGreenBlueAlpha);
                        }
                    }
                    tile.UnpackedTex = t;
                }
            }
        }

        public AtlasData(Stream stream, bool leaveOpen)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen);
           
            if ("BATL"u8.SequenceEqual(reader.ReadBytes(4)))
            {
                ReadFromBinary(reader);
            }
            else
            {
                stream.Position -= 4;
                ReadFromText(Encoding.UTF8.GetString(reader.ReadBytes((int)stream.Length)));
            }
        }
    }
}
