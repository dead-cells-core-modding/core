using HashlinkNET.Compiler.Pseudocode.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.SSA
{
    internal class IR_SSA_Save(
        SSARegisterData dst,
        IRResult value,
        string? assign
        ) :
        IRBase(value)
    {
        public readonly SSARegisterData dst = dst;
        public readonly IRResult value = value;
        public readonly string? assign = assign;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container,
            ILProcessor il )
        {
            if (value.IsNone)
            {
                return null;
            }
            var vt = value.Emit(ctx, true);
            if (ctx.RequestValue)
            {
                il.Emit(OpCodes.Dup);
            }
            var reg = dst.reg;
            if (reg != null)
            {
                if (reg.Kind == HlFuncRegisterData.RegisterKind.Parameter)
                {
                    il.Emit(OpCodes.Starg, reg.Parameter);
                }
                else
                {
                    il.Emit(OpCodes.Stloc, reg.Variable);
                }
            }
            if (ctx.RequestValue)
            {
                return vt;
            }
            return null;
        }
    }
}
