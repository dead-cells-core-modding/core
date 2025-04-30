using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Imports
{
    internal class ImportFuncTypeStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type ) => type is HlTypeWithFun;
        public override void Execute( IDataContainer data, HlCode code, GlobalData gdata,
            RuntimeImports rdata, HlType t )
        {
            var ftypes = data.GetGlobalData<FunctionTypes>();
            var ft = (HlTypeWithFun)t;

            var func = ft.FunctionDescription;
            var ret = func.ReturnType.Value!;
            TypeReference type;
            if (ret.Kind == HlTypeKind.Void)
            {
                if (func.Arguments.Length == 0)
                {
                    type = ftypes.ActionTypes[0];
                }
                else
                {
                    type = new GenericInstanceType(ftypes.ActionTypes[func.Arguments.Length]);
                }
                
            }
            else
            {
                type = new GenericInstanceType(ftypes.FuncTypes[func.Arguments.Length]);
            }

            data.AddData(t, type);
        }
    }
}
