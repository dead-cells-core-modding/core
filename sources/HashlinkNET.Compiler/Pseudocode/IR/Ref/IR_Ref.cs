using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.IR;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Ref
{
    class IR_Ref(
        HlFuncRegisterData reg
        ) : IRBase
    {
        public HlFuncRegisterData reg = reg;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (reg.Kind == HlFuncRegisterData.RegisterKind.Parameter)
            {
                il.Emit(OpCodes.Ldarga, reg.Parameter);
            }
            else
            {
                il.Emit(OpCodes.Ldloca, reg.Variable);
            }
            return reg.RegisterType.MakeByReferenceType();
        }
    }
}
