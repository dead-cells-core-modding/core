using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_LoadConst(object? val) : IRBase
    {
        public object? value = val;
        public override bool IgnoreSideEffects => true;
        public override bool IsConstantCost => true;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (value == null)
            {
                il.Emit(OpCodes.Ldnull);
                return ctx.TypeSystem.Object;
            }
            else if (value is int @int)
            {
                il.Emit(OpCodes.Ldc_I4, @int);
                return ctx.TypeSystem.Int32;
            }
            else if (value is long @long)
            {
                il.Emit(OpCodes.Ldc_I8, @long);
                return ctx.TypeSystem.Byte;
            }
            else if (value is float @float)
            {
                il.Emit(OpCodes.Ldc_R4, @float);
                return ctx.TypeSystem.Single;
            }
            else if (value is double @double)
            {
                il.Emit(OpCodes.Ldc_R8, @double);
                return ctx.TypeSystem.Double;
            }
            else if (value is string @string)
            {
                il.Emit(OpCodes.Ldstr, @string);
                return ctx.TypeSystem.String;
            }
            else if (value is bool @bool)
            {
                il.Emit(OpCodes.Ldc_I4, @bool ? 1 : 0);
                return ctx.TypeSystem.Boolean;
            }
            return null;
        }
    }
}
