using HashlinkNET.Bytecode;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    abstract class IRBase( params IRResult?[] values )
    {
        public IRResult?[] Values
        {
            get;
        } = values;
        public TypeReference? Emit( EmitContext ctx, bool requestValue = false )
        {
            ctx.RequestValue = requestValue;
            var rt = Emit(ctx, ctx.DataContainer, ctx.IL);
            var hasRet = rt != null && (rt.Namespace != "System" || rt.Name != "Void");
            if (requestValue)
            {
                if (!hasRet)
                {
                    throw new InvalidOperationException();
                }
            }
            if (hasRet)
            {
                if (!requestValue)
                {
                    ctx.IL.Emit(OpCodes.Pop);
                    return null;
                }
            }
            return rt;
        }

        protected abstract TypeReference? Emit( EmitContext ctx, IDataContainer container, 
            ILProcessor il );
    }
}
