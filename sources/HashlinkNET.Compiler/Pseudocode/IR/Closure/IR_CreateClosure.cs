using HashlinkNET.Compiler.Data.Interfaces;
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
            TypeReference? selfType = null;
            if (self != null)
            {
                selfType = self.Emit(ctx, true);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }
            if (
                (
                (method.HasThis &&
                selfType != null &&
                method.DeclaringType == selfType)
                )
                )
            {
                if (isVirt)
                {
                    il.Emit(OpCodes.Dup);
                }
                il.Emit(isVirt ? OpCodes.Ldvirtftn : OpCodes.Ldftn, method);
                il.Emit(OpCodes.Newobj, container.GetData<IConstructable>(type).Construct);
                return type;
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
