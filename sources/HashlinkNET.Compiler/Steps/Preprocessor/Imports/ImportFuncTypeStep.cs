using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Imports
{
    internal class ImportFuncTypeStep : GenerateTypeCompileStep
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

            addedTypes.Add(new(type, AddTypeKind.AddToTypesList, t.TypeIndex));

            data.AddData(t, type);
        }
    }
}
