using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace HashlinkNET.Compiler.Steps
{
    internal partial class GenerateXmlDocsStep : CompileStep
    {
        [GeneratedRegex(@"`\d+<")]
        private static partial Regex GenericInstanceMarkRegex();
        private static string ProcessTypeName( TypeReference type )
        {
            if (type is not GenericInstanceType git)
            {
                return type.FullName;
            }
            var old = type.FullName;
            var result = GenericInstanceMarkRegex().Replace(type.FullName, "<");
            return result;
        }
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<GlobalData>();
            var hx = gdata.Config.HaxeDocument!;
            var xml = gdata.XmlDocument;

            xml.Add(new XElement("doc", 
                new XElement("assembly", 
                new XElement("name", gdata.Assembly.Name.Name)
                )
                )
                );

            var members = new XElement("members");
            xml.Root!.Add(members);

            XElement GenerateSee(string type, string? simpleName)
            {
                StringBuilder sb = new();
                if (type.StartsWith("hxd") ||
                    type.StartsWith("h2d") ||
                    type.StartsWith("h3d"))
                {
                    sb.Append("https://heaps.io/api/");
                }
                else
                {
                    sb.Append("https://api.haxe.org/");
                }
                sb.Append(type.Replace('.', '/'));
                sb.Append(".html");

                if (!string.IsNullOrEmpty(simpleName))
                {
                    sb.Append('#');
                    sb.Append(simpleName);
                }
               

                return new XElement("see", new XAttribute("href", sb.ToString()), "(Haxe Docs)\n");
            }

            XElement AddMember(char kind, string? type, string name, 
                params object[] summary)
            {
                var m = new XElement("member");
                members.Add(m);
                if (string.IsNullOrEmpty(type))
                {
                    m.Add(new XAttribute("name", "T:" + name));
                }
                else
                {
                    m.Add(new XAttribute("name", kind + $":{type}." + name.Replace('<', '{').Replace('>', '}')));
                }
                m.Add(new XElement("summary", summary));
                return m;
            }

            foreach (var v in gdata.Code.Types)
            {
                if (v is HlTypeWithObj obj && !string.IsNullOrEmpty(obj.Name))
                {
                    var nstaticName = obj.Name.Replace("$", "");
                    var d = hx.Types.FirstOrDefault(x => x.Name == nstaticName);
                    if (d == null)
                    {
                        continue;
                    }

                    GeneralUtils.ParseHlTypeName(obj.Name, out var ns, out var typeName);
                    var isStatic = obj.Name.Contains('$');

                    var def = container.GetTypeRef(v);
                    AddMember('T', null, def.FullName, GenerateSee(nstaticName, null), d.Doc);

                    foreach (var f in d.Members)
                    {
                        var doc = f.Doc.Split('\n').SelectMany(x => (object[])[x, new XElement("br")]).ToArray();
                        if (!f.IsFunction || isStatic)
                        {
                            var prop = def.Resolve().FindProperty(f.Name);
                            if (prop == null)
                            {
                                continue;
                            }
                            AddMember('P', def.FullName, f.Name, GenerateSee(nstaticName, f.Name), doc);
                        }
                        else
                        {
                            var method = def.Resolve().FindMethod(f.Name);

                            if (method == null)
                            {
                                continue;
                            }

                            StringBuilder? sb;

                            if (method.Parameters.Count > 0)
                            {
                                sb = new StringBuilder();
                                sb.Append('(');
                                sb.AppendJoin(',', method.Parameters.Select(x => ProcessTypeName(x.ParameterType)));
                                sb.Append(')');
                            }
                            else
                            {
                                sb = null;
                            }

                            AddMember('M', def.FullName, f.Name + (sb?.ToString() ?? ""), GenerateSee(nstaticName, f.Name), doc);
                        }
                    }
                }
            }
        }
    }
}
