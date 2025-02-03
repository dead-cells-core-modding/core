using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ModCore.BuildSystem.MSBuild.Tasks
{
    public class GenerateModInfo : Task
    {
        public string Template
        {
            get; set;
        }
        [Required]
        public string OutJson
        {
            get; set;
        }
        [Required]
        public string Items
        {
            get; set;
        }
        public override bool Execute()
        {
            JObject obj;
            if (File.Exists(Template))
            {
                obj = JObject.Parse(File.ReadAllText(Template));
            }
            else
            {
                obj = new JObject();
            }
            Directory.CreateDirectory(Path.GetDirectoryName(OutJson));
            var items = Items.Split(new string[]
            {
                ";"
            }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var v0 in items)
            {
                var v = v0.Trim();
                if (string.IsNullOrEmpty(v))
                {
                    continue;
                }
                var npos = v.IndexOf('=');
                var name = v.Substring(0, npos).Trim();
                var value = v.Substring(npos + 1).Trim();

                JToken token = obj;
                var path = name.Split(new string[]
                {
                    "."
                }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < path.Length - 1; i++)
                {
                    token = token[path[i]];
                }
                JToken val = value;
                if (value == "#array")
                {
                    val = new JArray();
                }
                else if (value == "#object")
                {
                    val = new JObject();
                }
                var lp = path[path.Length - 1];
                if (token is JArray array)
                {
                    if (lp == "#add")
                    {
                        array.Add(val);
                        continue;
                    }
                }
                token[lp] = val;
            }
            File.WriteAllText(OutJson, obj.ToString(Newtonsoft.Json.Formatting.Indented));
            return true;
        }
    }
}
