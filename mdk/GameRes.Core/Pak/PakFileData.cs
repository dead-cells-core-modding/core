using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameRes.Core.Pak
{
    public abstract class PakFileData
    {
        private class MemoryData(ReadOnlyMemory<byte> data) : PakFileData
        {
            public override ReadOnlyMemory<byte> Data => data;
        }
        private class StreamData(Stream stream, long offset, int size) : PakFileData
        {
            private ReadOnlyMemory<byte>? cachedData;
            public override ReadOnlyMemory<byte> Data
            {
                get
                {
                    if(cachedData != null)
                    {
                        return cachedData.Value;
                    }
                    lock (stream)
                    {
                        var data = new byte[size];
                        var oldPos = stream.Position;
                        stream.Position = offset;
                        stream.ReadExactly(data);
                        stream.Position = oldPos;
                        cachedData = data;
                        return data;
                    }
                }
            }
        }
        public abstract ReadOnlyMemory<byte> Data { get; }

        public static PakFileData Empty { get; } = new MemoryData(ReadOnlyMemory<byte>.Empty);

        public static implicit operator PakFileData(ReadOnlyMemory<byte> data)
        {
            return new MemoryData(data);
        }
        public static implicit operator PakFileData(byte[] data)
        {
            return new MemoryData(data);
        }
        public static implicit operator ReadOnlyMemory<byte>(PakFileData data)
        {
            return data.Data;
        }
        public static PakFileData CreateFromStream(Stream stream, long start, int len)
        {
            return new StreamData(stream, start, len);
        }
    }
}
