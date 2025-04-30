using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps
{
    internal abstract class ForeachHlTypeCompileStep : ParallelCompileStep<HlType>
    {
        private RuntimeImports? rdata;
        private GlobalData? gdata;
        public virtual bool Filter( HlType type ) => true;
        public abstract void Execute(
            IDataContainer container,
            HlCode code,
            GlobalData gdata,
            RuntimeImports rdata,
            HlType type );
        protected override void Execute( IDataContainer container, HlType item, int index )
        {
            if (!Filter(item))
            {
                return;
            }

            Execute(container, gdata!.Code, gdata, rdata!, item);
        }
        protected override void Initialize( IDataContainer container )
        {
            base.Initialize(container);
            gdata = container.GetGlobalData<GlobalData>();
            rdata = container.GetGlobalData<RuntimeImports>();
        }
        protected override IReadOnlyList<HlType> GetItems( IDataContainer container )
        {
            return container.GetGlobalData<GlobalData>().Code.Types;
        }
    }
}
