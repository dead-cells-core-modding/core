using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Imports
{
    internal class ImportNullTypeStep : GenerateTypeCompileStep
    {
        public override bool Filter( HlType type ) => type.Kind == HlTypeKind.Null;


        public override void Execute( IDataContainer container, HlCode code,
            GlobalData gdata,
            RuntimeImports rdata,
            HlType type )
        {
            var tt = (HlTypeWithType)type;
            var et = container.GetTypeRef(tt.Type.Value);
            var rt = new GenericInstanceType(rdata.nullType)
            {
                GenericArguments =
                        {
                            et
                        }
            };
            addedTypes.Add(new(rt, AddTypeKind.AddToTypesList, type.TypeIndex));
            container.AddData(type, rt);
        }

    }
}
