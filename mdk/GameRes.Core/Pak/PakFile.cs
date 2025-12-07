using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GameRes.Core.Pak
{
    public class PakFile : IDisposable
    {
        public abstract class Entry
        {
            public string Name { get; set; } = "";
            public abstract bool IsDirectory { get; }
        }
        public class DirectoryEntry : Entry
        {
            public DirectoryEntry? GetDirectory(string path, [DoesNotReturnIf(true)] bool createWhenMissing = false)
            {
                if(string.IsNullOrEmpty(path))
                {
                    return this;
                }
                var parts = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
                DirectoryEntry cur = this;
                foreach (var v in parts)
                {
                    var entry = (DirectoryEntry?)cur.Entries.FirstOrDefault(x => x.Name == v);
                    if (entry == null)
                    {
                        if(!createWhenMissing)
                        {
                            return null;
                        }
                        entry = new DirectoryEntry()
                        {
                            Name = v
                        };
                        cur.Entries.Add(entry);
                    }
                    cur = entry;
                }
                return cur;
            }
            
            public Entry? GetEntry(string name)
            {
                var entry = Entries.FirstOrDefault(x => x.Name == name);
                if(entry != null)
                {
                    return entry;
                }
                var p = Path.GetDirectoryName(name) ?? "";
                var dir = GetDirectory(p, false);
                return dir?.GetEntry(Path.GetFileName(name));
            }
            public override bool IsDirectory => true;
            public List<Entry> Entries { get; set; } = [];
        }
        public class FileEntry : Entry
        {
            public void CalcChecksum()
            {
                Adler32 adler = new();
                adler.Make(Data.Data.Span);
                Checksum = adler.Value;
            }
            public int? Checksum { get; set; }
            public PakFileData Data { get; set; } = PakFileData.Empty;
            public override bool IsDirectory => false;
        }
        public Memory<byte> Stamp { get; set; }

        public DirectoryEntry Root { get; set; } = new();

        public DirectoryEntry GetOrCreateDirectory(string path)
        {
            return Root.GetDirectory(path, true);
        }

        public Entry? GetEntry(string path)
        {
            return Root.GetEntry(path);
        }

        public FileEntry? GetFile(string path)
        {
            return (FileEntry?) Root.GetEntry(path);
        }

        public DirectoryEntry? GetDirectory(string path)
        {
            return Root.GetDirectory(path, false);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write("PAK"u8);
            writer.Write(Stamp.IsEmpty ? (byte)0 : (byte)1);
            var posToHeaderSize = writer.BaseStream.Position;
            writer.Write(0);
            writer.Write(0);

            if(!Stamp.IsEmpty)
            {
                writer.Write(Stamp.Span[..64]); //Ensure the size of stamp
            }

            int dataOffset = 0;
            List<ReadOnlyMemory<byte>> dataSequence = [];

            void WriteEntry(Entry entry)
            {
                writer.Write((byte)entry.Name.Length);
                if (entry.Name.Length > 0)
                {
                    writer.Write(entry.Name.ToCharArray());
                }
                if(entry is DirectoryEntry dir)
                {
                    writer.Write((byte)1);
                    writer.Write(dir.Entries.Count);
                    foreach(var v in dir.Entries)
                    {
                        WriteEntry(v);
                    }
                }
                else if(entry is FileEntry file)
                {
                    
                    writer.Write((byte)0);
                    writer.Write(dataOffset);
                    writer.Write(file.Data.Data.Length);

                    if(file.Checksum == null)
                    {
                        file.CalcChecksum();
                    }

                    Debug.Assert(file.Checksum != null);
                    writer.Write(file.Checksum.Value);

                    dataOffset += file.Data.Data.Length;
                    dataSequence.Add(file.Data.Data);
                }
                else
                {
                    throw new InvalidProgramException();
                }
            }

            WriteEntry(Root);

            writer.Write("DATA"u8);

            var curPos = writer.BaseStream.Position;
            writer.BaseStream.Position = posToHeaderSize;
            writer.Write((int)curPos);
            writer.Write(dataOffset);
            writer.BaseStream.Position = curPos;

            foreach(var v in dataSequence)
            {
                writer.Write(v.Span);
            }
        }

        private Stream? inputStream;
        private MemoryMappedFile? mappedFile;
        private bool leaveOpen = false;
        private UnmanagedMemoryManager? memoryManager;

        public PakFile()
        {

        }

        public unsafe PakFile(Stream input, bool leaveOpen = false, bool allowUseMemporyMappedFile = true)
        {
            ArgumentNullException.ThrowIfNull(input);

            this.leaveOpen = leaveOpen;
            inputStream = input;

            Memory<byte> fileMemoryCopy = new();
            if(allowUseMemporyMappedFile && input is FileStream fs)
            {
                mappedFile = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read,
                    HandleInheritability.None, leaveOpen);

                input = mappedFile.CreateViewStream(fs.Position, fs.Length - fs.Position, MemoryMappedFileAccess.Read);
            }
            if(input is MemoryMappedViewStream mmvs)
            {
                byte* ptr = null;
                mmvs.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                memoryManager = new(ptr + mmvs.Position, (int)(mmvs.Length - mmvs.Position), new { pak = this,  mmvs,  input });
                fileMemoryCopy = memoryManager.Memory;
            }

            using var reader = new BinaryReader(input, Encoding.UTF8, true);
            ReadFrom(reader, fileMemoryCopy);
        }
        private unsafe void ReadFrom(BinaryReader reader, ReadOnlyMemory<byte> fileMemoryCopy)
        {
            var basePos = reader.BaseStream.Position;
            var pak = this;

            if(!
                "PAK"u8.SequenceEqual(reader.ReadBytes(3))
                )
            {
                throw new InvalidDataException();
            }

            var version = reader.ReadByte();
            var headerSize = reader.ReadInt32();
            var dataSize = reader.ReadInt32();

            if(version == 1) //Read Stamp
            {
                pak.Stamp = reader.ReadBytes(64);
            }

            Entry ReadEntry()
            {
                var nameLen = reader.ReadByte();
                var name = new string(reader.ReadChars(nameLen));
                var kind = reader.ReadByte();

                if (kind == 1)
                {
                    var dir = new DirectoryEntry()
                    {
                        Name = name,
                    };
                    var entriesCount = reader.ReadInt32();
                    for (int i = 0; i < entriesCount; i++)
                    {
                        dir.Entries.Add(ReadEntry());
                    }
                    return dir;
                }
                else
                {
                    long pos;
                    if ((kind & 2) == 2)
                    {
                        pos = reader.ReadInt64();
                    }
                    else
                    {
                        pos = reader.ReadInt32();
                    }
                    var size = reader.ReadInt32();
                    var checksum = reader.ReadInt32();

                    PakFileData d;
                    if (fileMemoryCopy.IsEmpty)
                    {
                        d = PakFileData.CreateFromStream(reader.BaseStream, basePos + headerSize + pos, size);
                    }
                    else
                    {
                        d = fileMemoryCopy[(int)(headerSize + pos)..(int)(headerSize + pos + size)];
                    }

                    return new FileEntry()
                    {
                        Name = name,
                        Checksum = checksum,
                        Data = d
                    };
                }
            }

            pak.Root = (DirectoryEntry) ReadEntry();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (!leaveOpen)
            {
                inputStream?.Dispose();
            }
            mappedFile?.Dispose();
            
        }
    }
}
