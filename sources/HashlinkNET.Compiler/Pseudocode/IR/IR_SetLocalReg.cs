using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_SetLocalReg(
        HlFuncRegisterData? dst,
        IRResult src
        ) : IRBaseWithSideEffects(src)
    {
        public HlFuncRegisterData? dst = dst;
        public readonly IRResult src = src;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var srcType = src.Emit(ctx, dst != null)!;

            if (dst == null)
            {
                return null;
            }

            if (srcType.FullName != dst.RegisterType.FullName)
            {
                new IR_Cast(new IR_LoadFromStack(srcType), dst.RegisterType, false).Emit(ctx, true);
            }

            if (dst.Kind == HlFuncRegisterData.RegisterKind.Parameter)
            {
                il.Emit(OpCodes.Starg, dst.Parameter);
            }
            else
            {
                Debug.Assert(dst.Variable != null);
                il.Emit(OpCodes.Stloc, dst.Variable);
            }
            return null;
        }
    }
}



