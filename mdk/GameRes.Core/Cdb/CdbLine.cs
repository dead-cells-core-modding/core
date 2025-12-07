using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameRes.Core.Cdb
{
    public class CdbLine
    {
        public string? Name { get; set; }
        public JObject Value { get; set; } = [];

        public CdbSheet Sheet => Separator.Sheet;
        public required CdbSeparator Separator { get; set; }

        public override string? ToString()
        {
            return Name;
        }
    }
}
