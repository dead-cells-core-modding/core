using HashlinkNET.Compiler.Pseudocode.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_LoadLocalReg(
        HlFuncRegisterData? src
        ) : IRBase
    {
        public HlFuncRegisterData? src = src; 
        public override bool IgnoreSideEffects => true;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (src == null)
            {
                il.Emit(OpCodes.Ldnull);
                return ctx.TypeSystem.Object; 
            }
            else if (src.Kind == HlFuncRegisterData.RegisterKind.Parameter)
            {
                il.Emit(OpCodes.Ldarg, src.Parameter);
            }
            else
            {
                il.Emit(OpCodes.Ldloc, src.Variable);
            }
            return src.RegisterType;
        }
    }
}
