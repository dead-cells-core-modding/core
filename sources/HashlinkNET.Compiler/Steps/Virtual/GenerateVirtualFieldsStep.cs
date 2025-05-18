using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Virtual
{
    internal class GenerateVirtualFieldsStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Virtual;
        }
        public override void Execute( IDataContainer container, HlCode code, GlobalData gdata, 
            RuntimeImports rdata, HlType type )
        {
            var ot = (HlTypeWithVirtual)type;
            var info = container.GetData<VirtualClassData>(type);
            var td = info.TypeDef;
            var obj = ot.Virtual;
            int fid = 0;
            var fields = info.Fields;

            foreach (var f in obj.Fields)
            {
                var fd = new PropertyDefinition(f.Name, PropertyAttributes.None,
                    container.GetTypeRef(f.Type.Value));
                if (string.IsNullOrEmpty(f.Name))
                {
                    fd.Name = "unnamedField" + fid++;
                }

                td.EmitFieldGetterSetter(fd, container, f.Name);

                td.Properties.Add(fd);
                fields.Add(fd);
                container.AddData(f, fd);
            }
        }
    }
}
