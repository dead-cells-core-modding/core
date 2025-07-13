using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.DynObj
{
    class IR_GetDynObj(
        IRResult src,
        string name
        ) : IRBase(src)
    {
        public readonly IRResult src = src;
        public string name = name;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            il.Emit(OpCodes.Ldstr, name);
            il.Emit(OpCodes.Call, ctx.RuntimeImports.phDynGetMethod);
            return ctx.TypeSystem.Object;
        }
    }
}
