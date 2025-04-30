using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Imports
{
    internal class ImportRefTypeStep : ForeachHlTypeCompileStep
    {

        public override bool Filter( HlType type ) => type.Kind == HlTypeKind.Ref;
        public override void Execute( IDataContainer container, 
            HlCode code, GlobalData gdata, 
            RuntimeImports rdata, HlType type )
        {
            var tt = (HlTypeWithType) type;
            var et = container.GetTypeRef(tt.Type.Value);
            container.AddData(type, rdata.refType.MakeGenericInstanceType(et));
        }
    }
}
