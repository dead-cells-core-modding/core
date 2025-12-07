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
            return cdb;
        }
    }
}
