
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func.Native
{
    internal class GenerateNativeFieldsDefStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<GlobalData>();
            var ncls = container.GetGlobalData<NativeImplClasses>();

            foreach (var v in gdata.Code.Natives)
            {
                var td = ncls.NativeImplClass[v.Lib];
                var fd = td.Fields.FirstOrDefault(x => x.Name == v.Name);
                if (fd != null)
                {
                    container.AddData(v, fd);
                    continue;
                }
                fd = new FieldDefinition(v.Name, FieldAttributes.Public | FieldAttributes.Static,
                    container.GetTypeRef(v.Type.Value));
                container.AddDataEach(v, fd);
                td.Fields.Add(fd);
            }
        }
    }
}
