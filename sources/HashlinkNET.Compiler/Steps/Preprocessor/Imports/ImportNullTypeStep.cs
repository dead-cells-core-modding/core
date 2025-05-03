using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Imports
{
    internal class ImportNullTypeStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type ) => type.Kind == HlTypeKind.Null;


        public override void Execute( IDataContainer container, HlCode code,
            GlobalData gdata,
            RuntimeImports rdata,
            HlType type )
        {
            var tt = (HlTypeWithType)type;
            var et = container.GetTypeRef(tt.Type.Value);
            container.AddData(type, new GenericInstanceType(rdata.nullType)
            {
                GenericArguments =
                        {
                            et
                        }
            });
        }

    }
}
