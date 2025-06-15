using HashlinkNET.Compiler.Pseudocode.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.SSA
{
    internal class IR_SSA_Load(
        SSARegisterData src
        ) : 
        IRBase(new IRResult())
    {
        public IRResult Value => Values[0]!;
        public readonly SSARegisterData src = src;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, 
            ILProcessor il )
        {
            if (!Value.IsNone)
            {
                return Value.Emit(ctx, true);
            }

            var reg = src.reg;
            if (reg == null)
            {
                il.Emit(OpCodes.Ldnull);
                return ctx.TypeSystem.Object;
            }
            else if (reg.Kind == HlFuncRegisterData.RegisterKind.Parameter)
            {
                il.Emit(OpCodes.Ldarg, reg.Parameter);
            }
            else
            {
                il.Emit(OpCodes.Ldloc, reg.Variable);
            }
            return reg.RegisterType;
        }
    }
}
