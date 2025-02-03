using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.BuildSystem
{
    internal class BuildModInfo(string template, string outJson, string itemStr)
    {
        public int Execute()
        {
            Console.WriteLine($"Template JSON: {template}");
            Console.WriteLine($"Output JSON: {outJson}");
            JObject obj;
            if (File.Exists(template))
            {
                obj = JObject.Parse(File.ReadAllText(template));
            }
            else
            {
                obj = [];
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outJson));
            var items = itemStr.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var v in items)
            {
                var npos = v.IndexOf('=');
                var name = v[..npos];
                var value = v[(npos + 1)..];

                JToken token = obj;
                var path = name.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var p in path.SkipLast(1))
                {
                    token = token[p];
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
                var lp = path.Last();
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
            File.WriteAllText(outJson, obj.ToString(Newtonsoft.Json.Formatting.Indented));
            return 0;
        }
    }
}
