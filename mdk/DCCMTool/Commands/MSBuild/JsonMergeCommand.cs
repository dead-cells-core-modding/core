using CommandLine;
using DCCMTool.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands.MSBuild
{
    internal class JsonMergeCommand : CommandBase<JsonMergeCommand.Options>
    {
        [Verb("merge-json", Hidden = true)]
        public class Options
        {
            [Option('i', "input")]
            public IEnumerable<string>? Inputs { get; set; }
            [Option('b', "base64")]
            public string? InputBase64 { get; set; }
            [Option('o', "output", Required = true)]
            public required string Output { get; set; }
        }

        public override async Task ExecuteAsync()
        {
            List<JObject> inputs = [];

            if (Arguments.Inputs != null)
            {
                foreach (var v in Arguments.Inputs)
                {
                    inputs.Add(JObject.Parse(await File.ReadAllTextAsync(v)));
                }
            }
            if (Arguments.InputBase64 != null)
            {
                var bytes = Convert.FromBase64String(Arguments.InputBase64);
                var str = Encoding.UTF8.GetString(bytes);
                var parts = str.Split("\"\"\"", StringSplitOptions.RemoveEmptyEntries);
                foreach (var v in parts)
                {
                    inputs.Add(JObject.Parse(v.Trim()));
                }
            }

            JObject root = [];

            static void Merge(JToken src, JToken dst)
            {
                if(src is JObject sobj)
                {
                    var dobj = (JObject)dst;
                    foreach(var name in sobj.Properties().Select(x => x.Name))
                    {
                        var dval = dobj[name];
                        var sval = sobj[name];

                        Debug.Assert(sval != null);

                        if(dval?.Type != sval.Type ||
                            sval.Type != JTokenType.Object ||
                            sval.Type != JTokenType.Array)
                        {
                            dval = sval;
                            goto WRITE_BACK;
                        }

                        Merge(sval, dval);

                        WRITE_BACK:
                        dobj[name] = dval;
                    }
                }
                else if(src is JArray sarray)
                {
                    var darray = (JArray)dst;
                    for(int i = 0; i < sarray.Count; i++)
                    {
                        var sval = sarray[i];
                        darray.Add(sval);
                    }
                }
            }

            foreach(var v in inputs)
            {
                Merge(v, root);
            }

            await File.WriteAllTextAsync(Arguments.Output, root.ToString(Newtonsoft.Json.Formatting.Indented));
        }
    }
}
