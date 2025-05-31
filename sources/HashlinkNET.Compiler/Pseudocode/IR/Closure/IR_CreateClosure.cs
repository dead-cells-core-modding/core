using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Closure
{
    internal class IR_CreateClosure(
        MethodReference method,
        TypeReference type,
        bool isVirt,
        IRResult? self
        ) : IRBase(self)
    {
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (self != null)
            {
                self.Emit(ctx, true);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }
            if (isVirt)
            {
                //il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldtoken, method);
            }
            else
            {
                il.Emit(OpCodes.Ldtoken, method);
            }
            il.Emit(OpCodes.Call, ctx.RuntimeImports.phCreateClosure.MakeInstance(type));
            return type;
        }
    }
}
