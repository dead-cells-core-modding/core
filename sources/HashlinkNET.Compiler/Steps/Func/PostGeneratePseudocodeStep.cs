using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func
{
    internal class PostGeneratePseudocodeStep : ForeachHlTypeCompileStep
    {
        public override void Execute( IDataContainer container, HlCode code,
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            if (!container.TryGetData<ITypeDefinitionValue>(type, out var td) ||
                td.TypeDef == null)
            {
                return;
            }
            foreach (var p in td.TypeDef.Properties)
            {
                p.GetMethod?.Body.Instructions.Clear();
                p.SetMethod?.Body.Instructions.Clear();
            }
            td.TypeDef.Fields.Clear();
        }
    }
}
