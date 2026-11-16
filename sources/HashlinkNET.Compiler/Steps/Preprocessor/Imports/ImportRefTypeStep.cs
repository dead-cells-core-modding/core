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

            // Main method signatures/locals use 'ref T' (a managed pointer).
            // Function type generic arguments still use Ref<T>; see
            // FixFuncTypeRefStep.
            container.AddData(type, et.MakeByReferenceType());
            addedTypes.Add(new(et.MakeByReferenceType(), AddTypeKind.AddToTypesList, type.TypeIndex));
        }
    }
}
