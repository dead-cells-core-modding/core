using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Object
{
    class IR_GetField(
        IRResult obj,
        PropertyDefinition field
        ) : IRBase(obj)
    {
        public readonly IRResult obj = obj;
        public PropertyDefinition field = field;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (obj.IsNone)
            {
                il.Emit(OpCodes.Call, field.GetMethod);
            }
            else
            {
                obj.Emit(ctx, true);
                il.Emit(OpCodes.Call, field.GetMethod);
            }
            return field.PropertyType;
        }
    }
}
