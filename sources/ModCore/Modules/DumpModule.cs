using Hashlink;
using ModCore.Hashlink;
using ModCore.Modules.Events;
using ModCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    internal unsafe class DumpModule : CoreModule<DumpModule>, IOnBeforeGameStartup
    {
        private static readonly FolderInfo dumpOutput = new("CORE_DUMP", "{CORE_ROOT}/dump");
        public void OnBeforeGameStartup()
        {
            if(!Core.Config.Value.EnableDump)
            {
                return;
            }

            Core.Config.Value.EnableDump = false;
            DoDump();
        }
        public void DoDump()
        {
            Logger.Information("Dumping");

            List<string> allLines = [];
            Dictionary<int, nint> func = [];
            var code = HashlinkVM.Instance.Context->code;
            StringBuilder sb = new();

            Logger.Information("Collecting functions");

            for(int i = 0; i < code->nfunctions; i++)
            {
                var f = code->functions + i;
                func[f->findex] = (nint)f;
            }

            

            for (int i = 0; i < code->ntypes; i++)
            {
                var t = code->types + i;
                if(t->kind != HL_type.TypeKind.HOBJ)
                {
                    continue;
                }
                var obj = t->data.obj;
                var name = HashlinkUtils.GetString(obj->name) + "@" + i;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }
                Logger.Information("Dumping type: {type}", name);
                allLines.Add("");
                allLines.Add("");
                allLines.Add("================================================");
                allLines.Add("type " + name);
                for(int j =0; j < obj->nproto; j++)
                {
                    sb.Clear();

                    var p = obj->proto + j;
                    var f = (HL_function*) func[p->findex];
                    var fn = HashlinkUtils.GetString(p->name) + "@" + p->findex;

                    sb.Append("    fn ");
                    sb.Append(fn);
                    sb.Append('(');

                    var tf = f->type->data.func;
                    for (int k = 0; k < tf->nargs; k++)
                    {
                        var arg = tf->args[k];
                        if(k > 0)
                        {
                            sb.Append(", ");
                        }
                        sb.Append(HashlinkUtils.GetTypeString(arg));
                    }

                    sb.Append(") -> ");
                    sb.Append(HashlinkUtils.GetTypeString(tf->ret));
                    allLines.Add(sb.ToString());
                }
                for (int j = 0; j < obj->nfields; j++)
                {
                    sb.Clear();
                    var f = obj->fields + j;
                    sb.Append("    field ");
                    sb.Append(HashlinkUtils.GetString(f->name));
                    sb.Append(": ");
                    sb.Append(HashlinkUtils.GetTypeString(f->t));

                    allLines.Add(sb.ToString());
                }
            }

            File.WriteAllLines(dumpOutput.GetFilePath("dump.txt"), allLines);

            Logger.Information("Dumpped");
        }
    }
}
