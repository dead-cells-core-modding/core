using HashlinkNET.Compiler.Data.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_GetGlobal(
        IGlobalValue globalValue
        ) : IRBase
    {
        public IGlobalValue globalValue = globalValue;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (globalValue.GlobalClassProp is not null)
            {
                il.Emit(OpCodes.Call, globalValue.GlobalClassProp.GetMethod);
                return globalValue.GlobalClassType;
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
                return ctx.TypeSystem.Object;
                //throw new InvalidOperationException();
            }
        }
    }
}
