using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.EnumOpt
{
    internal class IR_MakeEnum(
        MethodReference ctor,
        params IRResult[] args
        ) : IRBase(args)
    {
        public readonly IRResult[] args = args;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            Debug.Assert(ctor.Parameters.Count == args.Length);
            foreach (var v in args)
            {
                v.Emit(ctx, true);
            }
            il.Emit(OpCodes.Newobj, ctor);
            return ctor.DeclaringType;
        }
    }
}
