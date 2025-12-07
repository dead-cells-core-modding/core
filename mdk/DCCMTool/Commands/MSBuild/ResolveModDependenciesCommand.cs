using CommandLine;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands.MSBuild
{
    internal class ResolveModDependenciesCommand : CommandBase<ResolveModDependenciesCommand.Options>
    {
        [Verb("resolve-mod-deps", Hidden = true)]
        public class Options
        {
            [Option('i', "input", Required = true)]
            public required IEnumerable<string> Inputs { get; set; }
            [Option('r', "root", Required = true)]
            public required string ModsRoot { get; set; }
        }
        
        public record class ResolvedMod(string Name, string Version, string ModInfoPath, 
            string ModRoot, string? RequestVersion);

        public override void Execute()
        {
            List<ResolvedMod> result = [];
            bool hasMissing = false;
            foreach (var v in Arguments.Inputs)
            {
                var fullname = v.Split('-');
                Version? version = null;
                if (fullname.Length > 2 ||

                    fullname.Length == 2 && !Version.TryParse(fullname[1], out version)

                    )
                {
                    Console.Error.WriteLine("error INN : {0} is an invalid mod name", v);
                    hasMissing = true;
                    continue;
                }

                string name = fullname[0];
                var root = Path.GetFullPath(Path.Combine(Arguments.ModsRoot, name));
                var mip = Path.Combine(root, "modinfo.json");
                if (!File.Exists(mip))
                {
                    Console.Error.WriteLine("error MNF : The specific mod '{0}' was not found", name);
                    hasMissing = true;
                    continue;
                }
                var modinfo = JObject.Parse(File.ReadAllText(mip));
                if (modinfo["name"]?.ToString() != name)
                {
                    Console.Error.WriteLine("{0}: error IDS : ModInfo does not match directory structure", mip);
                    hasMissing = true;
                    continue;
                }
                Version modVer = Version.Parse(modinfo["version"]!.ToString());
                if (version != null)
                {
                    if (modVer < version)
                    {
                        Console.Error.WriteLine("{0}: error VER : The requested mod version {1} is greater than the existing mod version {2}", fullname[0], version, modVer);
                        hasMissing = true;
                        continue;
                    }
                }
                result.Add(new(
                
                    Name: fullname[0],
                    Version: modVer.ToString(),
                    ModInfoPath: mip,
                    ModRoot: root,
                    RequestVersion: version?.ToString()
                    
                ));

            }
        
            if(hasMissing)
            {
                return;
            }

            foreach(var v in result)
            {
                Console.WriteLine($"{v.Name};{v.Version};{v.ModRoot}");
            }
        }
    }
}
