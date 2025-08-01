using MemoryPack;
using MemoryPack.Compression;

namespace BytecodeMapping
{
    [MemoryPackable]
    public partial class BytecodeMappingData
    {
        [MemoryPackable]
        public partial class FunctionData
        {
            [MemoryPackable]
            public partial struct Item
            {
                [MemoryPackIgnore]
                public string? Path { get; set; }
                [MemoryPackInclude]
                internal int PathIndex { get; set; }
                public int Line { get; set; }
                public int ILIndex { get; set; }
            }
            public int FunctionIndex { get; set; }
            public List<Item> Instructions { get; set; } = [];
            public string Name { get; set; } = "";
        }

        public Dictionary<int, FunctionData> Functions { get; set; } = [];

        [MemoryPackInclude]
        private List<string?> StringMap { get; set; } = [];

        [MemoryPackOnSerializing]
        private void OnBeforeSerialize()
        {
            StringMap ??= [];
            StringMap.Clear();

            Dictionary<string, int> stringMap = [];

            foreach ((var _, var fun) in Functions)
            {
                for (int i = 0; i < fun.Instructions.Count; i++)
                {
                    var inst = fun.Instructions[i];
                    int id;
                    if(inst.Path == null)
                    {
                        id = -1;
                    }
                    else if(!stringMap.TryGetValue(inst.Path, out id))
                    {
                        id = StringMap.Count;
                        stringMap.Add(inst.Path, id);
                        StringMap.Add(inst.Path);
                    }
                    inst.PathIndex = id;
                    fun.Instructions[i] = inst;
                }
            }
        }

        [MemoryPackOnDeserialized]
        private void OnAfterDeserialize()
        {
            foreach ((var _, var fun) in Functions)
            {
                for (int i = 0; i < fun.Instructions.Count; i++)
                {
                    var inst = fun.Instructions[i];
                    if (inst.PathIndex == -1)
                    {
                        inst.Path = null;
                    }
                    else
                    {
                        inst.Path = StringMap[inst.PathIndex];
                    }
                    fun.Instructions[i] = inst;
                }
            }
        }
        public static BytecodeMappingData ReadFrom(ReadOnlySpan<byte> data)
        {
            using BrotliDecompressor decompressor = new BrotliDecompressor();
            return MemoryPackSerializer.Deserialize<BytecodeMappingData>(decompressor.Decompress(data)) ??
                throw new InvalidOperationException();
        }
        public byte[] Write()
        {
            //return MemoryPackSerializer.Serialize(this);
            using BrotliCompressor compressor = new(System.IO.Compression.CompressionLevel.Optimal);
            MemoryPackSerializer.Serialize(compressor, this);
            return compressor.ToArray();
        }
    }
}
