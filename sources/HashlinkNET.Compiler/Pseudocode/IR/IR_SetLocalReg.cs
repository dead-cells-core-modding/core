using HashlinkNET.Compiler.Pseudocode.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_SetLocalReg(
        HlFuncRegisterData? dst,
        IRResult src
        ) : IRBase(src)
    {
        public HlFuncRegisterData? dst = dst;
        public readonly IRResult src = src;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, dst != null);

            if (dst == null)
            {
                return null;
            }
            else if (dst.Kind == HlFuncRegisterData.RegisterKind.Parameter)
            {
                il.Emit(OpCodes.Starg, dst.Parameter);
            }
            else
            {
                il.Emit(OpCodes.Stloc, dst.Variable);
            }
            return null;
        }
    }
}
