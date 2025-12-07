using CommandLine;
using CommandLine.Text;
using GameRes.Core.Pak;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands.Pak
{
    internal class MergePakCommand : CommandBase<MergePakCommand.Options>
    {
        public override void Execute()
        {
            PakFile pak = new();

            if (!string.IsNullOrEmpty(Arguments.Stamp))
            {
                pak.Stamp = Encoding.ASCII.GetBytes(Arguments.Stamp).AsMemory()[..64];
            }

            foreach (var v in Arguments.Inputs)
            {
                PakFile.DirectoryEntry pakDir = pak.Root;
                string input = v;
                var splitIdx = v.IndexOf('=');
                if (splitIdx != -1)
                {
                    input = v[..splitIdx];
                    pakDir = pak.GetOrCreateDirectory(v[(splitIdx + 1)..]);
                }

                using var rs = File.OpenRead(input);
                var p = new PakFile(rs);

                void MergeEntry(PakFile.DirectoryEntry src, PakFile.DirectoryEntry dst)
                {
                    foreach (var v in src.Entries)
                    {
                        if (v is PakFile.DirectoryEntry dir)
                        {
                            var ddir = dst.GetDirectory(dir.Name, true);
                            MergeEntry(dir, ddir);
                        }
                        else if(v is PakFile.FileEntry file)
                        {
                            var fe = (PakFile.FileEntry?) dst.Entries.FirstOrDefault(x => x.Name == file.Name);
                            if(fe == null)
                            {
                                fe = new()
                                {
                                    Name = file.Name,
                                };
                                dst.Entries.Add(fe);
                            }
                            fe.Checksum = null;
                            fe.Data = file.Data;
                        }
                    }
                }

                MergeEntry(p.Root, pakDir);
            }

            if (Arguments.RemoveItems != null)
            {
                foreach (var v in Arguments.RemoveItems)
                {
                    var dir = Path.GetDirectoryName(v) ?? "";
                    var fn = Path.GetFileName(v);
                    var r = pak.Root.GetDirectory(dir, false);
                    r?.Entries.RemoveAll(x => x.Name == fn);
                }
            }

            if (Arguments.RemoveSameItems != null)
            {

                foreach (var v in Arguments.RemoveSameItems)
                {
                    using var istream = File.OpenRead(v);
                    PakFile template = new(istream);

                    bool isSHATemplate = template.Root.GetEntry(GenerateTemplatePakCommand.TEMPLATE_MARK_NAME) != null;

                    void RemoveSameFile(PakFile.DirectoryEntry src, PakFile.DirectoryEntry t)
                    {
                        foreach(var v in t.Entries)
                        {
                            if(v is PakFile.DirectoryEntry tdir)
                            {
                                var sd = src.GetDirectory(tdir.Name);
                                if(sd != null)
                                {
                                    RemoveSameFile(sd, tdir);
                                }
                            }
                            else if(v is PakFile.FileEntry tfile)
                            {
                                if (src.GetEntry(tfile.Name) is PakFile.FileEntry sf)
                                {
                                    if(sf.Checksum == tfile.Checksum)
                                    {
                                        var osha = isSHATemplate ? tfile.Data.Data.Span :
                                            SHA256.HashData(tfile.Data.Data.Span);
                                        if (tfile.Data.Data.Span.SequenceEqual(
                                            SHA256.HashData(sf.Data.Data.Span)
                                            ))
                                        {
                                            src.Entries.Remove(sf);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    RemoveSameFile(pak.Root, template.Root);
                }
            }
            using var stream = File.OpenWrite(Arguments.Output);
            pak.Write(new(stream));
        }

        [Verb("merge-paks", HelpText = "Merge multiple PAK files into a single pak file.")]
        public class Options
        {
            [Option('i', "input", Required = true, HelpText = "The path to the input pak files.")]
            public required IEnumerable<string> Inputs { get; set; }
            [Option('o', "output", Required = true, HelpText = "The path to the output pak file.")]
            public required string Output { get; set; }
            [Option("remove-items", HelpText = "Remove files from output pak file.")]
            public IEnumerable<string>? RemoveItems { get; set; }
            [Option("remove-same-items", HelpText = "Input the template pak file to remove duplicate entries.")]
            public IEnumerable<string>? RemoveSameItems { get; set; }
            [Option('s', "stamp", HelpText = "See https://n3rdl0rd.github.io/ModDocCE/files/pak/#stamps")]
            public string? Stamp { get; set; }

            [Usage]
            public static Example[] Examples => [
                new("Merge multiple PAK files into a single pak file", new Options(){
                    Inputs = ["res1.pak", "res2.pak", "res3.pak"],
                    Output = "output.pak"
                })
                ];
        }
    }
}
