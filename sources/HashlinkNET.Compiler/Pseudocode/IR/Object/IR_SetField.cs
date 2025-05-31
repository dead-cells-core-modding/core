using Mono.Cecil.Cil;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashlinkNET.Compiler.Pseudocode.IR;

namespace HashlinkNET.Compiler.Pseudocode.IR.Object
{
    class IR_SetField(
        IRResult obj,
        PropertyDefinition field,
        IRResult src
        ) : IRBase(obj, src)
    {
        public readonly IRResult obj = obj;
        public readonly IRResult src = src;
        public PropertyDefinition field = field;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (obj.IsNone)
            {
                src.Emit(ctx, true);
                il.Emit(OpCodes.Call, field.SetMethod);
            }
            else
            {
                obj.Emit(ctx, true);
                src.Emit(ctx, true);
                il.Emit(OpCodes.Call, field.SetMethod);
            }
            return null;
        }
    }
}
