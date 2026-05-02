using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Utils;

namespace HashlinkNET.Compiler.Steps.Func
{
    internal class FindCastExplicitTypesStep : ParallelCompileStep<HlFunction>
    {
        protected override IReadOnlyList<HlFunction> GetItems( IDataContainer container )
        {
            return container.GetGlobalData<GlobalData>().Code.Functions;
        }

        protected override void Execute( IDataContainer container, HlFunction item, int index )
        {
            foreach (var v in item.Opcodes)
            {
                if (v.Kind != HlOpcodeKind.SafeCast &
                    v.Kind != HlOpcodeKind.ToVirtual)
                {
                    continue;
                }

                var t1 = item.GetLocalRegType(v.Data[1]);
                var t2 = item.GetLocalRegType(v.Data[2]);

                void Process( HlType a, HlType b )
                {
                    if (a.Kind != HlTypeKind.Obj)
                    {
                        return;
                    }
                    if (b.Kind != HlTypeKind.Obj && b.Kind != HlTypeKind.Virtual)
                    {
                        return;
                    }
                    var info = container.GetData<ObjClassData>(a);
                    var another = container.GetData<ITypeReferenceValue>(b).TypeRef;
                    info.CastExplicitTypes.TryAdd(another, b.Kind == HlTypeKind.Virtual);
                }
                Process(t1, t2);
                Process(t2, t1);
            }
        }
    }
}
