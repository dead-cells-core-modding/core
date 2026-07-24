using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil.Rocks;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Imports
{
    internal class ImportRefTypeStep : GenerateTypeCompileStep
    {

        public override bool Filter( HlType type ) => type.Kind == HlTypeKind.Ref;
        public override void Execute( IDataContainer container,
            HlCode code, GlobalData gdata,
            RuntimeImports rdata, HlType type )
        {
            var tt = (HlTypeWithType)type;
            var et = container.GetTypeRef(tt.Type.Value);
            if (gdata.Config.GeneratePseudocode)
            {
                container.AddData(type, et.MakeByReferenceType());
            }
            else
            {
                addedTypes.Add(new(rdata.refType.MakeGenericInstanceType(et), AddTypeKind.AddToTypesList, type.TypeIndex));
                container.AddData(type, rdata.refType.MakeGenericInstanceType(et));
            }
        }
    }
}
