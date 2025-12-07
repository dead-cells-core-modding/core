using CommandLine;
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

namespace DCCMTool.Commands.Docs
{
    internal class GenerateHaxeDBCommand : CommandBase<GenerateHaxeDBCommand.Options>
    {
        private static async Task<string> SendCommand(NetworkStream stream, string command, bool requestResult = false)
        {
            await stream.WriteAsync(Encoding.UTF8.GetBytes(command + "\n\x00"));
            await stream.FlushAsync();

            using var buffer = new MemoryStream();
            var buf = ArrayPool<byte>.Shared.Rent(4096);

            while (true)
            {
                var count = await stream.ReadAsync(buf);
                if(count == 0)
                {
                    break;
                }

                buffer.Write(buf, 0, count);
            }

            ArrayPool<byte>.Shared.Return(buf);

            var result = buffer.ToArray();
            var str = Encoding.UTF8.GetString(result, 0, result.Length);
            
            return str;
        }
        private static async Task<string> Display(NetworkStream stream, string fn, int pos, string? type = null)
        {
            return (await SendCommand(stream, $"--display {fn}@{pos}{(string.IsNullOrEmpty(type) ? "" : $"@{type}")}", true)).Trim();
        }
        private async Task<XmlDocument> Display(NetworkStream stream, string text, string? type = null)
        {
            await File.WriteAllTextAsync(Path.Combine(Arguments.TempDir!, "Main.tx"), text);
            var str = await Display(stream, "Main.hx", text.Length, type);
            Console.WriteLine(str);
            var doc = new XmlDocument();
            doc.LoadXml(str);
            return doc;
        }

        public override async Task ExecuteAsync()
        {
            var port = Arguments.Port;
            var td = Arguments.TempDir;
            if(string.IsNullOrEmpty(td))
            {
                td = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }
            Arguments.TempDir = td = Path.GetFullPath(td);
            Directory.CreateDirectory(td);


            Console.WriteLine($"Starting haxe language server at port {port}");

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

            Process? proc = null;
            Console.WriteLine("Connecting to haxe language server...");

            _RE_TRY:
            try
            {
                await socket.ConnectAsync(endPoint);
            }
            catch (Exception)
            {
                if(proc != null)
                {
                    proc.Kill();
                    throw;
                }
                proc = Process.Start(new ProcessStartInfo()
                {
                    FileName = "haxe",
                    Arguments = $" -v --wait {port} "
                });
                goto _RE_TRY;
            }

            try
            {
                Console.WriteLine("Connected to haxe language server");
                using NetworkStream stream = new(socket, true);

                await SendCommand(stream, $"--cwd \"{td}\"");
                await SendCommand(stream, "--lib heaps");
                await Display(stream, "import ");

            }
            finally
            {
                proc?.Kill();
            }
            await Task.Delay(-1);
        }
        [Verb("generate-haxe-db", Hidden = true)]
        public class Options
        {
            [Option('o', "output", HelpText = "The path to the output HaxeDB file.", Required = true)]
            public required string Output { get; set; }
            [Option('t', "temp-dir", HelpText = "The path to a temporary directory to use during generation.", Required = false)]
            public string? TempDir { get; set; }
            [Option('p', "port", HelpText = "The port to use for the Haxe language server.", Required = false, Default = 6003)]
            public int Port { get; set; } = 6002;
        }
    }
}
