using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Virtual
{
    internal class FixVirtualNameStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            Dictionary<string, SortedList<string, VirtualClassData>> virtuals = [];
            var gdata = container.GetGlobalData<GlobalData>();

            foreach (var v in gdata.Code.Types)
            {
                if (v is not HlTypeWithVirtual vt)
                {
                    continue;
                }
                var info = container.GetData<VirtualClassData>(vt);

                if (!virtuals.TryGetValue(info.ShortName, out var list))
                {
                    list = [];
                    virtuals.Add(info.ShortName, list);
                }

                list.Add(info.FullName + v.TypeIndex, info);
            }

            foreach ((var name, var list) in virtuals)
            {
                if (list.Count == 1)
                {
                    continue;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    var td = list.GetValueAtIndex(i).TypeDef;
                    td.Name = name + "_" + i;
                }
            }
        }
    }
}
