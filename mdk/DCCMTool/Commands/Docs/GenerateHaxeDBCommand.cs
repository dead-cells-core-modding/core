using CommandLine;
using HaxeDocs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DCCMTool.Commands.Docs
{
    internal class GenerateHaxeDBCommand : CommandBase<GenerateHaxeDBCommand.Options>
    {

        [Serializable]
        public class HaxeCompilerException : Exception
        {
            public HaxeCompilerException() { }
            public HaxeCompilerException(string message) : base(message) { }
            public HaxeCompilerException(string message, Exception inner) : base(message, inner) { }
            
        }
        private record class HaxeInstance(BinaryReader OutputStream, BinaryWriter InputStream, Process Process, int Index);
        private static async Task<string> SendCommand(HaxeInstance haxe, string? stdin, params List<string> args)
        {
            using BinaryWriter writer = new(new MemoryStream());

            if(!string.IsNullOrEmpty(stdin))
            {
                args.Add("-D");
                args.Add("display-stdin");
            }

            foreach(var v in args)
            {
                writer.Write(Encoding.UTF8.GetBytes(v));
                writer.Write('\n');
            }

            if(!string.IsNullOrEmpty(stdin))
            {
                writer.Write((byte)1);
                writer.Write(Encoding.UTF8.GetBytes(stdin));
            }

            haxe.InputStream.Write((int)writer.BaseStream.Length);

            writer.BaseStream.Position = 0;
            await writer.BaseStream.CopyToAsync(haxe.InputStream.BaseStream);

            // Wait result

            var lenBuffer = new byte[4];
            await haxe.OutputStream.BaseStream.ReadAtLeastAsync(lenBuffer, 4, false);
            var resultLen = BitConverter.ToInt32(lenBuffer);
            var resultBuffer = new byte[resultLen];
            await haxe.OutputStream.BaseStream.ReadAtLeastAsync(resultBuffer, resultLen, false);
            var resultStr = Encoding.UTF8.GetString(resultBuffer);
            StringBuilder sb = new();
            foreach(var l in resultStr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (l[0] == 0x01)
                {
                    Console.WriteLine("Haxe Print: " + l[1..]);
                }
                else if (l[0] == 0x02)
                {
                    Console.Error.WriteLine("Haxe Error: " + l[1..]);

                }
                else
                {
                    sb.AppendLine(l);
                }
            }

            //Console.WriteLine("Result: " + sb);
            return sb.ToString();
        }

        private int requestIndex = 0;

        private string[] commonDisplayLibraryArgs = [];
        
        private async Task<JObject> Display(HaxeInstance haxe, 
            string? stdin, 
            string method, 
            object data, 
            IEnumerable<string>? extraArgs = null)
        {
            JObject jobj = [];
            jobj["jsonrpc"] = "2.0";
            jobj["id"] = Interlocked.Increment(ref requestIndex);
            jobj["method"] = method;
            jobj["params"] = JObject.FromObject(data);
            var str = jobj.ToString(Newtonsoft.Json.Formatting.None);
            var result = await SendCommand(haxe, stdin,
                [
                ..(extraArgs ?? []),
                ..(commonDisplayLibraryArgs ?? []),
                $"--display", str
                ]);

            var json = JObject.Parse(result);
            if (json.TryGetValue("error", out var err))
            {
                throw new HaxeCompilerException(err.ToString());
            }
            return (JObject)json.Get("result").Get("result");
        }
        private async Task<JObject> Display(HaxeInstance haxe, string text, IEnumerable<string>? extraArgs = null, string? type = null)
        {
            var fp = Path.Combine(Arguments.TempDir!, $"Main{haxe.Index}.tx");
            if (!File.Exists(fp))
            {
                await File.WriteAllTextAsync(fp, "");
            }
            return await Display(haxe, null, "display/completion", new { offset = text.Length, contents = text, 
                file = fp, wasAutoTriggered = false });
        }
        private Task<JObject> DisplayFunc(HaxeInstance haxe, string text, IEnumerable<string>? extraArgs = null, string? type = null)
        {
            return Display(haxe, "function main() { " + text, extraArgs, type);
        }

        private int haxeInstanceIndex = 0;

        private HaxeInstance CreateHaxeInstance()
        {
            Process? proc = Process.Start(new ProcessStartInfo("haxe", " --wait stdio ")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true
            });

            Debug.Assert(proc != null);

            return new(new(proc.StandardError.BaseStream), new(proc.StandardInput.BaseStream), proc, Interlocked.Increment(ref haxeInstanceIndex));
        }

        private static string GetFullName(JToken obj)
        {
            StringBuilder sb = new();

            string[]? packs;
            var path = obj["path"];
            if(path != null)
            {
                packs = path.Get("pack").ToObject<string[]?>();
            }
            else
            {
                packs = obj.Get("pack").ToObject<string[]?>();
            }

            if (packs != null)
            {
                foreach (var v in packs)
                {
                    sb.Append(v);
                    sb.Append('.');
                }
            }
            sb.Append(obj["path"]?["typeName"] ?? obj["name"]);
            return sb.ToString();
        }

        private async Task ScanTypeMembers(HaxeDocument.TypeInfo[] typeNames, HaxeInstance? haxe)
        {
            if(haxe == null)
            {
                haxe = CreateHaxeInstance();
            }
            
            foreach(var cinfo in typeNames)
            {
                async Task LoadMemberInfo(bool isStatic)
                {
                    try
                    {
                        JObject rxml;
                        if (isStatic)
                        {
                            rxml = await DisplayFunc(haxe, cinfo.Name + ".");
                        }
                        else
                        {
                            rxml = await DisplayFunc(haxe, "var a:" + cinfo.Name + ";a.");
                        }
                        Debug.Assert(rxml != null);
                        foreach (var item in rxml.Get("items"))
                        {
                            if (item.GetString("kind") == "ClassField")
                            {
                                var origin = GetFullName(item.Get("args").Get("origin").Get("args"));

                                if(origin != cinfo.Name)
                                {
                                    if(!cinfo.Inheritances.Contains(origin))
                                    {
                                        cinfo.Inheritances.Add(origin);
                                    }
                                    continue;
                                }

                                var field = item.Get("args").Get("field");


                                cinfo.Members.Add(new()
                                {
                                    Name = field.GetString("name"),
                                    Doc = field.GetString("doc"),
                                    IsFunction = field.Get("kind").GetString("kind") == "FMethod",
                                    IsStatic = isStatic
                                });
                            }
                        }

                    }
                    catch (JsonReaderException) { }
                    catch (HaxeCompilerException ex)
                    {
                        Console.Error.WriteLine(ex);
                    }
                }

                await LoadMemberInfo(true);
                await LoadMemberInfo(false);
            }
            haxe.Process.Kill();
            Console.WriteLine("Job Finished");
        }

        public override async Task<int> ExecuteAsync()
        {
            var jobsCount = Arguments.Jobs ?? 1;
            if(jobsCount <= 0)
            {
                jobsCount = Environment.ProcessorCount;
            }

            Console.WriteLine("Jobs Count: " + jobsCount);

            var td = Arguments.TempDir;
            if (string.IsNullOrEmpty(td))
            {
                td = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }
            Arguments.TempDir = td = Path.GetFullPath(td);
            Directory.CreateDirectory(td);

            if(Arguments.Libraries != null)
            {
                Console.WriteLine("Parsing libraries...");
                var hxlib = Process.Start(new ProcessStartInfo("haxelib", " path " + string.Join(' ', Arguments.Libraries)){
                    RedirectStandardOutput = true,
                });
                await hxlib!.WaitForExitAsync();
                var parts = hxlib.StandardOutput.ReadToEnd().Split(['\n', '\r'], 
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                StringBuilder sb = new();
                foreach(var v in parts)
                {
                    if(!v.StartsWith('-'))
                    {
                        sb.Append("-cp ");
                    }
                    sb.AppendLine(v);
                }
                Console.WriteLine(sb);
                commonDisplayLibraryArgs = [ sb.ToString() ];
            }
            
            Console.WriteLine("Starting haxe language server...");

            var defHaxe = CreateHaxeInstance();

            dynamic initResult = (await Display(defHaxe, null, "initialize", new { }));

            var version = new Version((int)initResult["haxeVersion"]["major"],
                (int)initResult["haxeVersion"]["minor"],
                (int)initResult["haxeVersion"]["patch"]);

            Console.WriteLine("Haxe Version: " + version);



            //Collecting class

            Console.WriteLine("Collecting class..");

            var doc = new HaxeDocument();
            List<HaxeDocument.TypeInfo> types = doc.Types;

            {
                var allTypeResult = await Display(defHaxe, "import ");

                foreach (var v in allTypeResult.Get("items"))
                {
                    string tkind = v.GetString("kind");

                    if (tkind == "Type")
                    {
                        var args = v.Get("args");
                        var name = GetFullName(args);

                        types.Add(new()
                        {
                            Doc = args.GetString("doc"),
                            Name = name,
                            Kind = args.Get<int>("kind")
                        });
                    }
                }
            }

            Console.WriteLine($"Loaded {types.Count} types");
            await DisplayFunc(defHaxe, "var a: h3d.mat.Texture3D;a.");

            Console.WriteLine("Loading class members...");

            List<Task> jobs = [];

            int typesPerJob = types.Count / jobsCount;
            int typeIndex = 0;

            var typesArray = types.ToArray();

            for(int i = 0; i < jobsCount - 1; i++)
            {
                jobs.Add(ScanTypeMembers(typesArray[typeIndex..(typeIndex + typesPerJob)], null));
                typeIndex += typesPerJob;
            }

            if(typeIndex < typesArray.Length)
            {
                jobs.Add(ScanTypeMembers(typesArray[typeIndex..], defHaxe));
            }

           

            await Task.WhenAll(jobs);

            await File.WriteAllTextAsync(Arguments.Output, JsonConvert.SerializeObject(doc));
            return 0;
        }
        [Verb("generate-haxe-db", Hidden = true)]
        public class Options
        {
            [Option('o', "output", HelpText = "The path to the output HaxeDB file.", Required = true)]
            public required string Output { get; set; }
            [Option('l', "library", HelpText = "")]
            public IEnumerable<string>? Libraries { get; set; }
            [Option('j', "jobs", HelpText = "", Default = 1)]
            public int? Jobs { get; set; }
            [Option('t', "temp-dir", HelpText = "The path to a temporary directory to use during generation.", Required = false)]
            public string? TempDir { get; set; }
        }
    }
}
