using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameRes.Core.Cdb
{
    public class CdbSheet
    {
        private static readonly string[] NAME_JSON_ID = [ "id", "item", "name", "room", "animId" ];
        public string Name { get; set; } = "";
        public JArray Columns { get; set; } = [];
        public JObject? Props { get; set; }
        public List<CdbSeparator> Separators { get; set; } = [];
        public static CdbSheet ReadFrom(JObject root)
        {
            var sheet = new CdbSheet
            {
                Name = root["name"]!.ToString(),
                Columns = (JArray)root["columns"]!,
                Props = (JObject?)root["props"]
            };

            List<(CdbSeparator, int)> separators;

            if (sheet.Props != null &&
                sheet.Props.Remove("separatorTitles", out var separatorTitlesToken) &&
                ((JArray)separatorTitlesToken!).Count > 0)
            {
                var array = (JArray)separatorTitlesToken!;
                for(int i = 0; i < array.Count; i++)
                {
                    var sep = new CdbSeparator()
                    {
                        Name = array[i].ToString(),
                        Sheet = sheet,
                    };
                    sheet.Separators.Add(sep);
                }
                separators = [..root["separators"]!.ToObject<int[]>()!.Select((val, idx) =>
                {
                    return (sheet.Separators[idx], val);
                }).OrderBy(x => x.val)];
                if (separators[0].Item2 != 0)
                {
                    var sep = new CdbSeparator()
                    {
                        Sheet = sheet
                    };
                    sheet.Separators.Insert(0, sep);
                    separators.Insert(0, (sep, 0));
                }
            }
            else
            {
                var sep = new CdbSeparator()
                {
                    Sheet = sheet
                };
                sheet.Separators.Add(sep);
                separators = [(sep, 0)];
            }

            var curSep = 0;

            var lines = (JArray)root["lines"]!;
            for(int i= 0; i < lines.Count; i++)
            {
                if(curSep != separators.Count - 1)
                {
                    if(i >= separators[curSep + 1].Item2)
                    {
                        curSep++;
                    }
                }

                var sep = separators[curSep].Item1;
                var line = (JObject)lines[i];
                var l = new CdbLine()
                {
                    Value = line,
                    Separator = sep
                };
                foreach(var v in NAME_JSON_ID)
                {
                    if(line.TryGetValue(v, out var id))
                    {
                        l.Name = id.ToString();
                        break;
                    }
                }

                sep.Lines.Add(l);
            }

            return sheet;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
