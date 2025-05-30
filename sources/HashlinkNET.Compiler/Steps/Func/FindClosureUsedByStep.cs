using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func
{
    internal class FindClosureUsedByStep : ParallelCompileStep<HlFunction>
    {
        private GlobalData gdata = null!;
        private RuntimeImports rdata = null!;

        protected override void Initialize( IDataContainer container )
        {
            base.Initialize(container);

            gdata = container.GetGlobalData<GlobalData>();
            rdata = container.GetGlobalData<RuntimeImports>();

        }
        protected override void Execute( IDataContainer container, HlFunction item, int index )
        {
            var selfData = container.GetData<FuncData>(item);
            var closureCount = 0;
            foreach (var v in item.Opcodes)
            {
                if (v.Kind < HlOpcodeKind.StaticClosure ||
                    v.Kind > HlOpcodeKind.InstanceClosure)
                {
                    continue;
                }
                var fid = gdata.Code.FunctionIndexes[v.Parameters[1]];
                if (fid >= gdata.Code.Functions.Count)
                {
                    continue; //Native
                }
                var func = gdata.Code.Functions[fid];
                var fdata = container.GetData<FuncData>(func);
                fdata.UsedBy.Add((selfData, closureCount++));
            }
        }

        protected override IReadOnlyList<HlFunction> GetItems( IDataContainer container )
        {
            return container.GetGlobalData<GlobalData>().Code.Functions;
        }
    }
}
