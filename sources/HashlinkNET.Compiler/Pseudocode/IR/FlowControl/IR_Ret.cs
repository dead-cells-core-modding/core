using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace HashlinkNET.Compiler.Pseudocode.IR.FlowControl
{
    class IR_Ret(
        IRResult retValue
        ) : IRBase(retValue)
    {
        public IRResult retValue = retValue;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var st = retValue.Emit(ctx, !retValue.IsNone);

            if (!retValue.IsNone)
            {
                Debug.Assert(st != null );
                if (st.FullName != ctx.Definition.ReturnType.FullName)
                {
                    new IR_Cast(new IR_LoadFromStack(st), ctx.Definition.ReturnType, false).Emit(ctx, true);
                }
            }

            il.Emit(OpCodes.Ret);
            return null;
        }
    }
}
