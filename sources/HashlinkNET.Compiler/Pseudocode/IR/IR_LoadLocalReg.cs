using HashlinkNET.Compiler.Pseudocode.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_LoadLocalReg(
        HlFuncRegisterData? src,
        string? assignName = null
        ) : IRBase
    {
        public HlFuncRegisterData? src = src;
        public readonly string? assignName = assignName;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (src == null)
            {
                il.Emit(OpCodes.Ldnull);
                return ctx.TypeSystem.Object;
            }
            else if (src.Kind == HlFuncRegisterData.RegisterKind.Parameter)
            {
                // Cecil's ThisParameter has index -1 and cannot be encoded as
                // an ldarg operand, so load 'this' explicitly.
                if (src.Parameter == null || src.Parameter.Index < 0)
                {
                    il.Emit(OpCodes.Ldarg_0);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg, src.Parameter);
                }
            }
            else
            {
                il.Emit(OpCodes.Ldloc, src.Variable);
            }
            return src.RegisterType;
        }
    }
}
