using Mono.Cecil;
using Mono.Cecil.Rocks;
using MonoMod.Utils;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Linq;

namespace DCCMTool.Commands.Core
{
    internal class GenerateSubAssemblyCommand : CommandBase<GenerateSubAssemblyCommand.Settings>
    {
        private class AssemblyInfo
        {
            public AssemblyDefinition Definition { get; set; }

            public Dictionary<string, List<TypeDefinition>> Types { get; } = [];
            public Dictionary<string, List<IMemberDefinition>> Members { get; } = [];

            public AssemblyInfo(AssemblyDefinition definition)
            {
                Definition = definition;

                foreach(var v in definition.MainModule.GetAllTypes())
                {
                    {
                        if (!Types.TryGetValue(v.FullName, out var tlist))
                        {
                            tlist = [];
                            Types.Add(v.FullName, tlist);
                        }
                        tlist.Add(v);
                    }

                    List<(string, IMemberDefinition)> members = [];
                    foreach(var f in v.Fields)
                    {
                        members.Add(("F:" + f.FullName, f));
                    }
                    foreach(var m in v.Methods)
                    {
                        members.Add(("M:" + m.FullName, m));
                       
                    }
                    foreach(var p in v.Properties)
                    {
                        members.Add(("P:" + p.FullName, p));
                    }
                    foreach(var e in v.Events)
                    {
                        members.Add(("E:" + e.FullName, e));
                    }

                    foreach((var key, var def) in members)
                    {
                        if (!Members.TryGetValue(key, out var list))
                        {
                            list = [];
                            Members.Add(key, list);
                        }
                        list.Add(def);
                    }
                }
            }
        }
        public override int Execute()
        {
            AssemblyInfo[] assemblyInfos = new AssemblyInfo[Arguments.Input.Length];

            for(int i = 0; i < Arguments.Input.Length; i++)
            {
                assemblyInfos[i] = new(AssemblyDefinition.ReadAssembly(Arguments.Input[i]));
            }

            var template = assemblyInfos[0];

            foreach ((var fullName, var tdl) in template.Types)
            {
                foreach (var v in assemblyInfos)
                {
                    if(!v.Types.ContainsKey(fullName))
                    {
                        foreach (var td in tdl)
                        {
                            if (td.IsNested)
                            {
                                td.DeclaringType.NestedTypes.Remove(td);
                            }
                            else
                            {
                                td.Module.Types.Remove(td);
                            }
                        }
                    }
                }
            }

            foreach((var fullName, var mdl) in template.Members)
            {
                foreach(var v in assemblyInfos)
                {
                    if(!v.Members.ContainsKey(fullName))
                    {
                        foreach (var md in mdl)
                        {
                            var td = md.DeclaringType;
                            if (md is FieldDefinition fd)
                            {
                                td.Fields.Remove(fd);
                            }
                            else if (md is MethodDefinition m)
                            {
                                td.Methods.Remove(m);
                            }
                            else if (md is EventDefinition e)
                            {
                                td.Events.Remove(e);
                            }
                            else if (md is PropertyDefinition p)
                            {
                                td.Properties.Remove(p);
                            }
                        }
                    }
                }
            }

            var def = template.Definition;

            def.Name.Name = "GameProxy";

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Arguments.Output))!);

            using var fs = File.OpenWrite(Arguments.Output);
            def.Write(fs);

            return 0;
        }
        public class Settings : CommandSettings
        {
            [CommandOption("-i|--inputs", true)]
            public required string[] Input { get; set; }

            [CommandOption("-o|--output", true)]
            public required string Output { get; set; }
        }
    }
}
