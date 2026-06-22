using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameRes.Core.Cdb
{
    public class CdbFile
    {
        public Dictionary<string, JToken?> Metadata { get; set; } = [];
        public List<CdbSheet> Sheets { get; set; } = [];
        public static CdbFile ReadFrom(string jsonContent)
        {
            return ReadFrom((JObject)JToken.Parse(jsonContent));
        }
        public static CdbFile ReadFrom(JObject root)
        {
            var cdb = new CdbFile();
            foreach(var v in root["sheets"]!.AsJEnumerable())
            {
                var sheet = CdbSheet.ReadFrom((JObject)v);
                cdb.Sheets.Add(sheet);
            }
            foreach(var v in root.Properties())
            {
                if(v.Name == "sheets")
                {
                    continue;
                }
                cdb.Metadata.Add(v.Name, root[v.Name]);
            }
            return cdb;
        }

        public void WriteTo(JObject root)
        {
            var sheets = new JArray();

            foreach(var v in Sheets)
            {
                var sheet = new JObject();
                v.WriteTo(sheet);
                sheets.Add(sheet);
            }

            root["sheets"] = sheets;

            foreach((var key, var val) in Metadata)
            {
                root[key] = val;
            }
        }
    }
}
