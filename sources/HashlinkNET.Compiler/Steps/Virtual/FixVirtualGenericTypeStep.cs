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
    internal class FixVirtualGenericTypeStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Virtual;
        }
        public override void Execute( IDataContainer container, HlCode code, GlobalData gdata, 
            RuntimeImports rdata, HlType type )
        {
            var info = container.GetData<VirtualClassData>(type);
            var virt = ((HlTypeWithVirtual)type).Virtual;
            if (info.TypeRef is not GenericInstanceType git)
            {
                return;
            }
            var group = info.Group;
            if (group.Types.Count == 1)
            {
                return;
            }
            foreach (var v in group.TypeDef.GenericParameters)
            {
                var fn = v.Name;
                var f = virt.Fields.Where(x => x.Name == fn).First();
                git.GenericArguments.Add(container.GetTypeRef(f.Type.Value));
            }
        }
    }
}
