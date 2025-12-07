using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameRes.Core.Cdb
{
    public class CdbSeparator
    {
        public string? Name { get; set; }
        public List<CdbLine> Lines { get; set; } = [];
        public required CdbSheet Sheet { get; set; }
        public override string? ToString()
        {
            return Name;
        }
    }
}
